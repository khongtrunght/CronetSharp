using System;
using System.IO;
using System.Text;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class BodyTest
    {
        [Test]
        public void TestFromBytes()
        {
            var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
            var body = Body.FromBytes(expectedBytes);

            Assert.IsNotNull(body);
            Assert.AreEqual(5, body.Length());

            var actualBytes = body.AsBytes();
            Assert.IsNotNull(actualBytes);
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [Test]
        public void TestFromString()
        {
            var expectedString = "Hello, World!";
            var body = Body.FromString(expectedString);

            Assert.IsNotNull(body);

            var actualBytes = body.AsBytes();
            Assert.IsNotNull(actualBytes);

            var actualString = Encoding.UTF8.GetString(actualBytes);
            Assert.AreEqual(expectedString, actualString);
            Assert.AreEqual(Encoding.UTF8.GetByteCount(expectedString), body.Length());
        }

        [Test]
        public void TestEmpty()
        {
            var body = Body.Empty();

            Assert.IsNotNull(body);
            Assert.AreEqual(0, body.Length());

            var bytes = body.AsBytes();
            Assert.IsNotNull(bytes);
            Assert.AreEqual(0, bytes.Length);
        }

        [Test]
        public void TestFromStream()
        {
            var expectedBytes = new byte[] { 10, 20, 30, 40, 50 };
            var stream = new MemoryStream(expectedBytes);

            var body = Body.FromStream(stream, expectedBytes.Length);

            Assert.IsNotNull(body);
            Assert.AreEqual(expectedBytes.Length, body.Length());

            // Streams should not be buffered
            Assert.IsNull(body.AsBytes());

            // ReadAll should work for streams
            var actualBytes = body.ReadAll();
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [Test]
        public void TestFromStreamWithoutLength()
        {
            var expectedBytes = new byte[] { 100, 200 };
            var stream = new MemoryStream(expectedBytes);

            var body = Body.FromStream(stream);

            Assert.IsNotNull(body);
            Assert.IsNull(body.Length()); // Length is unknown
            Assert.IsNull(body.AsBytes()); // Not buffered
        }

        [Test]
        public void TestFromFile()
        {
            // Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var expectedContent = "File content test";
                File.WriteAllText(tempFile, expectedContent);

                var body = Body.FromFile(tempFile);

                Assert.IsNotNull(body);

                var fileInfo = new FileInfo(tempFile);
                Assert.AreEqual(fileInfo.Length, body.Length());

                // Streams should not be buffered
                Assert.IsNull(body.AsBytes());

                // ReadAll should work
                var actualBytes = body.ReadAll();
                var actualContent = Encoding.UTF8.GetString(actualBytes);
                Assert.AreEqual(expectedContent, actualContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void TestCloneBytesBody()
        {
            var originalBytes = new byte[] { 1, 2, 3 };
            var body = Body.FromBytes(originalBytes);

            var clonedBody = body.TryClone();

            Assert.IsNotNull(clonedBody);

            var clonedBytes = clonedBody.AsBytes();
            CollectionAssert.AreEqual(originalBytes, clonedBytes);

            // Ensure it's a deep copy
            Assert.AreNotSame(body.AsBytes(), clonedBytes);
        }

        [Test]
        public void TestCloneStreamBodyFails()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var body = Body.FromStream(stream);

            var clonedBody = body.TryClone();

            Assert.IsNull(clonedBody); // Streams cannot be cloned
        }

        [Test]
        public void TestImplicitConversionFromBytes()
        {
            byte[] bytes = new byte[] { 5, 10, 15 };
            Body body = bytes;

            Assert.IsNotNull(body);
            CollectionAssert.AreEqual(bytes, body.AsBytes());
        }

        [Test]
        public void TestImplicitConversionFromString()
        {
            string str = "Test string";
            Body body = str;

            Assert.IsNotNull(body);

            var actualString = Encoding.UTF8.GetString(body.AsBytes());
            Assert.AreEqual(str, actualString);
        }

        [Test]
        public void TestReadAllForBytesBody()
        {
            var expectedBytes = new byte[] { 7, 14, 21 };
            var body = Body.FromBytes(expectedBytes);

            var actualBytes = body.ReadAll();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [Test]
        public void TestReadAllForStreamBody()
        {
            var expectedBytes = new byte[] { 99, 88, 77 };
            var stream = new MemoryStream(expectedBytes);
            var body = Body.FromStream(stream);

            var actualBytes = body.ReadAll();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }
    }
}
