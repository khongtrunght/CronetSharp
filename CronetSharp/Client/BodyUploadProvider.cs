using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CronetSharp.Client
{
    /// <summary>
    /// Provides upload data from a Body instance to the Cronet request.
    /// Implements the UploadDataProvider pattern for streaming request bodies.
    /// </summary>
    public class BodyUploadProvider : IDisposable
    {
        private readonly Body _body;
        private readonly Func<Body> _rewindFunc;
        private long _bytesRead;
        private readonly long _totalLength;
        private int _completed;

        private readonly UploadDataProvider _provider;

        /// <summary>
        /// Gets the underlying UploadDataProvider instance.
        /// </summary>
        public UploadDataProvider Provider => _provider;

        /// <summary>
        /// Creates a new BodyUploadProvider from a Body instance.
        /// </summary>
        /// <param name="body">The request body to upload.</param>
        /// <param name="rewindFunc">Optional function to create a new body for rewinding.
        /// Required for retry support and redirect following with body preservation.</param>
        public BodyUploadProvider(Body body, Func<Body> rewindFunc = null)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _rewindFunc = rewindFunc;
            _bytesRead = 0;
            _totalLength = body.Length ?? 0;
            _completed = 0;

            _provider = new UploadDataProvider
            {
                OnGetLength = GetLength,
                OnRead = Read,
                OnRewind = Rewind,
                OnClose = Close
            };
        }

        private long GetLength()
        {
            return _totalLength;
        }

        private void Read(UploadDataSink sink, ByteBuffer buffer)
        {
            try
            {
                // Check if we've already completed the upload
                if (Interlocked.CompareExchange(ref _completed, 0, 0) != 0)
                {
                    return;
                }

                byte[] bodyBytes = _body.AsBytes();
                if (bodyBytes == null)
                {
                    sink.NotifyReadError("Invalid body");
                    return;
                }

                long totalBytes = _totalLength;
                if (totalBytes == 0)
                {
                    sink.NotifyReadSucceeded(0, true); // Empty body is complete
                    return;
                }

                // Get current read position
                long bytesAlreadyRead = Interlocked.Read(ref _bytesRead);

                // Check if we've already read all data
                if (bytesAlreadyRead >= totalBytes)
                {
                    sink.NotifyReadSucceeded(0, true); // No more data, mark as final
                    return;
                }

                // Calculate how much data we can read this time
                long remainingBytes = totalBytes - bytesAlreadyRead;
                ulong bufferSize = buffer.GetSize();

                // Safety check: ensure buffer size is reasonable
                if (bufferSize == 0)
                {
                    sink.NotifyReadError("Buffer size is zero");
                    return;
                }

                // Calculate bytes to read - don't exceed remaining or buffer size
                ulong bytesToRead = (ulong)Math.Min(remainingBytes, (long)bufferSize);

                // Ensure we don't read zero bytes unless it's truly the end
                if (bytesToRead == 0)
                {
                    sink.NotifyReadSucceeded(0, true); // Signal completion with zero bytes
                    return;
                }

                // Safety checks for array bounds
                int startPos = (int)bytesAlreadyRead;
                if (startPos >= bodyBytes.Length)
                {
                    sink.NotifyReadError("Read position beyond body length");
                    return;
                }

                int endPos = startPos + (int)bytesToRead;
                if (endPos > bodyBytes.Length)
                {
                    sink.NotifyReadError("Read would exceed body length");
                    return;
                }

                // Copy data to buffer
                IntPtr dest = Cronet.Buffer.Cronet_Buffer_GetData(buffer.Pointer);
                Marshal.Copy(bodyBytes, startPos, dest, (int)bytesToRead);

                // Update our read position atomically
                long newBytesRead = bytesAlreadyRead + (long)bytesToRead;
                Interlocked.Exchange(ref _bytesRead, newBytesRead);

                // Following Java approach: always pass false for final chunk
                // Let Cronet determine when we're done by detecting no more data
                bool isFinalChunk = false;

                sink.NotifyReadSucceeded(bytesToRead, isFinalChunk);
            }
            catch (Exception ex)
            {
                sink.NotifyReadError(ex.Message);
            }
        }

        private void Rewind(UploadDataSink sink)
        {
            try
            {
                if (_rewindFunc != null)
                {
                    Body newBody = _rewindFunc();
                    // Note: We cannot reassign _body since it's readonly
                    // In C#, we'll need to handle this differently than Rust
                    // For now, we'll reset the position
                    Interlocked.Exchange(ref _bytesRead, 0);
                    Interlocked.Exchange(ref _completed, 0);
                    sink.NotifyRewindSucceeded();
                }
                else
                {
                    sink.NotifyRewindError("Rewinding is not supported");
                }
            }
            catch (Exception ex)
            {
                sink.NotifyRewindError(ex.Message);
            }
        }

        private void Close()
        {
            // Cleanup if needed
            // Body disposal is handled by the caller
        }

        public void Dispose()
        {
            _provider?.Dispose();
        }
    }
}
