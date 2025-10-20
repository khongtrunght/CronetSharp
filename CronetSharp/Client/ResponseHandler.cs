using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CronetSharp.Client
{
    /// <summary>
    /// Represents the result of an HTTP request.
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; internal set; }

        /// <summary>
        /// Gets the HTTP status text.
        /// </summary>
        public string StatusText { get; internal set; }

        /// <summary>
        /// Gets the response headers.
        /// </summary>
        public Dictionary<string, string[]> Headers { get; internal set; }

        /// <summary>
        /// Gets the response body.
        /// </summary>
        public Body Body { get; internal set; }

        /// <summary>
        /// Gets the final URL after following redirects.
        /// </summary>
        public string Url { get; internal set; }

        /// <summary>
        /// Gets whether the response was retrieved from cache.
        /// </summary>
        public bool WasCached { get; internal set; }

        /// <summary>
        /// Gets the negotiated protocol (e.g., "h2", "h3").
        /// </summary>
        public string NegotiatedProtocol { get; internal set; }

        internal HttpResponse()
        {
            Headers = new Dictionary<string, string[]>();
            Body = Client.Body.Empty();
        }
    }

    /// <summary>
    /// Status of the HTTP request operation.
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// Request completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// Request was canceled.
        /// </summary>
        Canceled,

        /// <summary>
        /// Request failed with an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// Handles HTTP response callbacks and accumulates response data.
    /// Implements the UrlRequestCallback pattern for the high-level client API.
    /// </summary>
    public class ResponseHandler : IDisposable
    {
        private readonly Func<string, bool> _shouldRedirect;
        private readonly TaskCompletionSource<(ResponseStatus, HttpResponse, ClientError)> _tcs;
        private readonly UrlRequestCallback _callback;
        private HttpResponse _response;
        private ulong _bufferSize;
        private List<byte> _bodyBytes;

        /// <summary>
        /// Gets the underlying UrlRequestCallback.
        /// </summary>
        public UrlRequestCallback Callback => _callback;

        /// <summary>
        /// Gets the task that completes when the request finishes.
        /// </summary>
        public Task<(ResponseStatus status, HttpResponse response, ClientError error)> Task => _tcs.Task;

        /// <summary>
        /// Creates a new ResponseHandler.
        /// </summary>
        /// <param name="shouldRedirect">Function to determine if a redirect should be followed.</param>
        public ResponseHandler(Func<string, bool> shouldRedirect = null)
        {
            _shouldRedirect = shouldRedirect ?? (url => true); // Follow all redirects by default
            _tcs = new TaskCompletionSource<(ResponseStatus, HttpResponse, ClientError)>();
            _response = new HttpResponse();
            _bufferSize = 512; // Default buffer size
            _bodyBytes = new List<byte>();

            _callback = new UrlRequestCallback
            {
                OnRedirectReceived = HandleRedirectReceived,
                OnResponseStarted = HandleResponseStarted,
                OnReadCompleted = HandleReadCompleted,
                OnSucceeded = HandleSucceeded,
                OnFailed = HandleFailed,
                OnCancelled = HandleCanceled
            };
        }

        /// <summary>
        /// Sets the buffer size for reading the response body.
        /// The default is 512 bytes.
        /// </summary>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        public void SetBufferSize(ulong bufferSize)
        {
            _bufferSize = bufferSize;
        }

        private void HandleRedirectReceived(UrlRequest request, UrlResponseInfo info, string newLocationUrl)
        {
            try
            {
                if (_shouldRedirect(newLocationUrl))
                {
                    request.FollowRedirect();
                }
                else
                {
                    // Store response info and complete
                    UpdateResponseFromInfo(info);
                    _tcs.TrySetResult((ResponseStatus.Success, _response, null));
                }
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void HandleResponseStarted(UrlRequest request, UrlResponseInfo info)
        {
            try
            {
                UpdateResponseFromInfo(info);
                Read(request);
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void HandleReadCompleted(UrlRequest request, UrlResponseInfo info, ByteBuffer buffer, ulong bytesRead)
        {
            try
            {
                if (bytesRead == 0)
                {
                    return;
                }

                // Append the data to our body buffer
                byte[] data = buffer.GetData();
                if (data != null && data.Length > 0)
                {
                    // Only take the bytes that were actually read
                    int actualBytesRead = (int)Math.Min(bytesRead, (ulong)data.Length);
                    for (int i = 0; i < actualBytesRead; i++)
                    {
                        _bodyBytes.Add(data[i]);
                    }
                }

                Read(request);
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void HandleSucceeded(UrlRequest request, UrlResponseInfo info)
        {
            try
            {
                // Finalize the response body
                _response.Body = Client.Body.FromBytes(_bodyBytes.ToArray());

                _tcs.TrySetResult((ResponseStatus.Success, _response, null));
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void HandleFailed(UrlRequest request, UrlResponseInfo info, CronetException error)
        {
            try
            {
                var clientError = ClientError.FromCronetError(error);
                _tcs.TrySetResult((ResponseStatus.Error, null, clientError));
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void HandleCanceled(UrlRequest request, UrlResponseInfo info)
        {
            try
            {
                _tcs.TrySetResult((ResponseStatus.Canceled, null, ClientError.FromCancellation()));
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult((ResponseStatus.Error, null, ClientError.FromException(ex)));
            }
        }

        private void Read(UrlRequest request)
        {
            var buffer = new ByteBuffer(_bufferSize);
            request.Read(buffer);
        }

        private void UpdateResponseFromInfo(UrlResponseInfo info)
        {
            _response.StatusCode = info.HttpStatusCode;
            _response.StatusText = info.HttpStatusCodeText;
            _response.Url = info.Url;
            _response.WasCached = info.WasCached;
            _response.NegotiatedProtocol = info.NegotiatedProtocol;

            // Convert headers to dictionary
            var headers = new Dictionary<string, List<string>>();
            foreach (var header in info.Headers)
            {
                string name = header.Name;
                string value = header.Value;

                if (!headers.ContainsKey(name))
                {
                    headers[name] = new List<string>();
                }
                headers[name].Add(value);
            }

            _response.Headers = headers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray()
            );
        }

        public void Dispose()
        {
            _callback?.Dispose();
        }
    }
}
