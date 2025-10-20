using System;
using System.Text;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class BodyUploadProviderTest
    {
        [Test]
        public void Constructor_WithNullBody_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BodyUploadProvider(null));
        }

        [Test]
        public void Constructor_WithValidBody_CreatesProvider()
        {
            var body = Body.FromBytes(new byte[] { 1, 2, 3 });
            using var provider = new BodyUploadProvider(body);

            Assert.IsNotNull(provider);
            Assert.IsNotNull(provider.Provider);
        }

        [Test]
        public void GetLength_ReturnsBodyLength()
        {
            var testData = Encoding.UTF8.GetBytes("test data");
            var body = Body.FromBytes(testData);
            using var provider = new BodyUploadProvider(body);

            long length = provider.Provider.OnGetLength();

            Assert.AreEqual(testData.Length, length);
        }

        [Test]
        public void GetLength_WithEmptyBody_ReturnsZero()
        {
            var body = Body.FromBytes(new byte[0]);
            using var provider = new BodyUploadProvider(body);

            long length = provider.Provider.OnGetLength();

            Assert.AreEqual(0, length);
        }

        [Test]
        public void Read_WithValidData_CopiesToBuffer()
        {
            var expected = Encoding.UTF8.GetBytes("test");
            var body = Body.FromBytes(expected);
            using var provider = new BodyUploadProvider(body);
            using var buffer = new ByteBuffer((ulong)expected.Length);

            ulong bytesRead = 0;
            bool isFinal = false;

            var sink = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) =>
                {
                    bytesRead = bytes;
                    isFinal = final;
                },
                OnReadError = (ex) => Assert.Fail($"Read failed: {ex.Message}")
            };

            provider.Provider.OnRead(sink, buffer);

            Assert.AreEqual((ulong)expected.Length, bytesRead);
            Assert.IsFalse(isFinal); // Should be false per Java approach

            var actual = buffer.GetData();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Read_WithEmptyBody_SignalsCompletion()
        {
            var body = Body.FromBytes(new byte[0]);
            using var provider = new BodyUploadProvider(body);
            using var buffer = new ByteBuffer(1024);

            ulong bytesRead = 0;
            bool isFinal = false;

            var sink = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) =>
                {
                    bytesRead = bytes;
                    isFinal = final;
                },
                OnReadError = (ex) => Assert.Fail($"Read failed: {ex.Message}")
            };

            provider.Provider.OnRead(sink, buffer);

            Assert.AreEqual(0UL, bytesRead);
            Assert.IsTrue(isFinal);
        }

        [Test]
        public void Read_MultipleReads_ReadsSequentially()
        {
            var testData = Encoding.UTF8.GetBytes("Hello World");
            var body = Body.FromBytes(testData);
            using var provider = new BodyUploadProvider(body);

            // Read first 5 bytes
            using var buffer1 = new ByteBuffer(5);
            ulong bytesRead1 = 0;
            var sink1 = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) => { bytesRead1 = bytes; },
                OnReadError = (ex) => Assert.Fail($"Read 1 failed: {ex.Message}")
            };

            provider.Provider.OnRead(sink1, buffer1);
            Assert.AreEqual(5UL, bytesRead1);

            var chunk1 = buffer1.GetData();
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello"), chunk1);

            // Read next 6 bytes
            using var buffer2 = new ByteBuffer(6);
            ulong bytesRead2 = 0;
            var sink2 = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) => { bytesRead2 = bytes; },
                OnReadError = (ex) => Assert.Fail($"Read 2 failed: {ex.Message}")
            };

            provider.Provider.OnRead(sink2, buffer2);
            Assert.AreEqual(6UL, bytesRead2);

            var chunk2 = buffer2.GetData();
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(" World"), chunk2);
        }

        [Test]
        public void Rewind_WithoutRewindFunc_ReturnsError()
        {
            var body = Body.FromBytes(new byte[] { 1, 2, 3 });
            using var provider = new BodyUploadProvider(body, rewindFunc: null);

            bool errorCalled = false;
            var sink = new UploadDataSink
            {
                OnRewindSucceeded = () => Assert.Fail("Rewind should not succeed"),
                OnRewindError = (ex) => { errorCalled = true; }
            };

            provider.Provider.OnRewind(sink);

            Assert.IsTrue(errorCalled);
        }

        [Test]
        public void Rewind_WithRewindFunc_ResetsPosition()
        {
            var testData = Encoding.UTF8.GetBytes("test");
            var body = Body.FromBytes(testData);
            Func<Body> rewindFunc = () => Body.FromBytes(testData);
            using var provider = new BodyUploadProvider(body, rewindFunc);

            // Read some data first
            using var buffer1 = new ByteBuffer((ulong)testData.Length);
            var sink1 = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) => { },
                OnReadError = (ex) => Assert.Fail($"Read failed: {ex.Message}")
            };
            provider.Provider.OnRead(sink1, buffer1);

            // Now rewind
            bool rewindSucceeded = false;
            var rewindSink = new UploadDataSink
            {
                OnRewindSucceeded = () => { rewindSucceeded = true; },
                OnRewindError = (ex) => Assert.Fail($"Rewind failed: {ex.Message}")
            };
            provider.Provider.OnRewind(rewindSink);

            Assert.IsTrue(rewindSucceeded);

            // Read again to verify position was reset
            using var buffer2 = new ByteBuffer((ulong)testData.Length);
            ulong bytesRead2 = 0;
            var sink2 = new UploadDataSink
            {
                OnReadSucceeded = (bytes, final) => { bytesRead2 = bytes; },
                OnReadError = (ex) => Assert.Fail($"Read after rewind failed: {ex.Message}")
            };
            provider.Provider.OnRead(sink2, buffer2);

            Assert.AreEqual((ulong)testData.Length, bytesRead2);
            var actual = buffer2.GetData();
            CollectionAssert.AreEqual(testData, actual);
        }

        [Test]
        public void Dispose_DisposesUnderlyingProvider()
        {
            var body = Body.FromBytes(new byte[] { 1, 2, 3 });
            var provider = new BodyUploadProvider(body);

            Assert.DoesNotThrow(() => provider.Dispose());

            // Second dispose should not throw
            Assert.DoesNotThrow(() => provider.Dispose());
        }
    }
}
