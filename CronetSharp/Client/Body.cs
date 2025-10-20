// This implementation is inspired by: https://github.com/seanmonstar/reqwest/blob/6792f697fcdb27c47dcbf7bd05f23368d1d4ac80/src/blocking/body.rs
// License: https://github.com/seanmonstar/reqwest/blob/master/LICENSE-MIT

using System;
using System.IO;
using System.Text;

namespace CronetSharp.Client
{
    /// <summary>
    /// Represents the body of an HTTP request or response.
    /// </summary>
    public class Body
    {
        private readonly BodyKind _kind;

        private Body(BodyKind kind)
        {
            _kind = kind;
        }

        /// <summary>
        /// Creates a new body from a stream reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A new Body instance.</returns>
        public static Body FromStream(Stream stream)
        {
            return new Body(new StreamKind(stream, null));
        }

        /// <summary>
        /// Creates a new body from a stream reader with a known length.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The length of the stream in bytes.</param>
        /// <returns>A new Body instance.</returns>
        public static Body FromStream(Stream stream, long length)
        {
            return new Body(new StreamKind(stream, length));
        }

        /// <summary>
        /// Creates a new body from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A new Body instance.</returns>
        public static Body FromBytes(byte[] bytes)
        {
            return new Body(new BytesKind(bytes));
        }

        /// <summary>
        /// Creates a new body from a string using UTF-8 encoding.
        /// </summary>
        /// <param name="str">The string content.</param>
        /// <returns>A new Body instance.</returns>
        public static Body FromString(string str)
        {
            return FromBytes(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Creates a new body from a file path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>A new Body instance.</returns>
        public static Body FromFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new Body(new StreamKind(stream, fileInfo.Length));
        }

        /// <summary>
        /// Creates an empty body.
        /// </summary>
        /// <returns>A new empty Body instance.</returns>
        public static Body Empty()
        {
            return new Body(new BytesKind(new byte[0]));
        }

        /// <summary>
        /// Returns the body as a byte array if the body is already buffered in memory.
        /// For streamed requests this method returns null.
        /// </summary>
        /// <returns>The byte array, or null if the body is streamed.</returns>
        public byte[] AsBytes()
        {
            return _kind.AsBytes();
        }

        /// <summary>
        /// Gets the length of the body in bytes, if known.
        /// </summary>
        /// <returns>The length in bytes, or null if unknown.</returns>
        public long? Length()
        {
            return _kind.Length();
        }

        /// <summary>
        /// Attempts to clone this body. Returns null if the body cannot be cloned (e.g., for streams).
        /// </summary>
        /// <returns>A cloned Body, or null if cloning is not supported.</returns>
        public Body TryClone()
        {
            var clonedKind = _kind.TryClone();
            return clonedKind != null ? new Body(clonedKind) : null;
        }

        /// <summary>
        /// Reads the entire body into a byte array.
        /// </summary>
        /// <returns>The complete body as a byte array.</returns>
        public byte[] ReadAll()
        {
            return _kind.ReadAll();
        }

        /// <summary>
        /// Implicit conversion from byte array to Body.
        /// </summary>
        public static implicit operator Body(byte[] bytes) => FromBytes(bytes);

        /// <summary>
        /// Implicit conversion from string to Body.
        /// </summary>
        public static implicit operator Body(string str) => FromString(str);

        // Internal abstract class for different body types
        private abstract class BodyKind
        {
            public abstract byte[] AsBytes();
            public abstract long? Length();
            public abstract BodyKind TryClone();
            public abstract byte[] ReadAll();
        }

        // Stream-based body
        private class StreamKind : BodyKind
        {
            private readonly Stream _stream;
            private readonly long? _length;

            public StreamKind(Stream stream, long? length)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _length = length;
            }

            public override byte[] AsBytes() => null; // Streams are not buffered

            public override long? Length() => _length;

            public override BodyKind TryClone()
            {
                // Streams cannot be cloned in general
                return null;
            }

            public override byte[] ReadAll()
            {
                if (_stream.CanSeek)
                {
                    _stream.Seek(0, SeekOrigin.Begin);
                }

                using (var memoryStream = new MemoryStream())
                {
                    _stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        // Byte array body
        private class BytesKind : BodyKind
        {
            private readonly byte[] _bytes;

            public BytesKind(byte[] bytes)
            {
                _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            }

            public override byte[] AsBytes() => _bytes;

            public override long? Length() => _bytes.Length;

            public override BodyKind TryClone()
            {
                // Create a copy of the byte array
                var cloned = new byte[_bytes.Length];
                Array.Copy(_bytes, cloned, _bytes.Length);
                return new BytesKind(cloned);
            }

            public override byte[] ReadAll() => _bytes;
        }
    }
}
