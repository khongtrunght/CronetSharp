using NUnit.Framework;
using CronetSharp.Client;
using CronetSharp.Cronet;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class ClientErrorTest
    {
        [Test]
        public void TestFromCronetError()
        {
            using var cronetException = new CronetException
            {
                ErrorCode = ErrorCode.ConnectionRefused,
                CronetErrorMessage = "Connection refused by server"
            };

            var error = ClientError.FromCronetError(cronetException);

            Assert.AreEqual(ClientErrorType.CronetError, error.ErrorType);
            Assert.IsTrue(error.IsCronetError);
            Assert.IsFalse(error.IsCancellation);
            Assert.IsFalse(error.IsEngineError);
            Assert.IsFalse(error.IsTimeout);
            Assert.IsNotNull(error.Message);
            Assert.IsTrue(error.Message.Contains("Connection refused"));
        }

        [Test]
        public void TestFromCancellation()
        {
            var error = ClientError.FromCancellation();

            Assert.AreEqual(ClientErrorType.CancellationError, error.ErrorType);
            Assert.IsFalse(error.IsCronetError);
            Assert.IsTrue(error.IsCancellation);
            Assert.IsFalse(error.IsEngineError);
            Assert.IsFalse(error.IsTimeout);
            Assert.IsTrue(error.Message.Contains("cancelled"));
        }

        [Test]
        public void TestFromEngineResult()
        {
            var result = EngineResult.ILLEGAL_ARGUMENT_INVALID_HOSTNAME;
            var error = ClientError.FromEngineResult(result);

            Assert.AreEqual(ClientErrorType.EngineError, error.ErrorType);
            Assert.IsFalse(error.IsCronetError);
            Assert.IsFalse(error.IsCancellation);
            Assert.IsTrue(error.IsEngineError);
            Assert.IsFalse(error.IsTimeout);
            Assert.IsNotNull(error.EngineResultCode);
            Assert.AreEqual(result, error.EngineResultCode.Value);
            Assert.IsTrue(error.Message.Contains("Unexpected engine result"));
            Assert.IsTrue(error.Message.Contains("INVALID_HOSTNAME"));
        }

        [Test]
        public void TestFromTimeout()
        {
            var error = ClientError.FromTimeout();

            Assert.AreEqual(ClientErrorType.TimeoutError, error.ErrorType);
            Assert.IsFalse(error.IsCronetError);
            Assert.IsFalse(error.IsCancellation);
            Assert.IsFalse(error.IsEngineError);
            Assert.IsTrue(error.IsTimeout);
            Assert.IsTrue(error.Message.Contains("timed out"));
        }

        [Test]
        public void TestToString()
        {
            var error = ClientError.FromTimeout();
            var str = error.ToString();

            Assert.IsNotNull(str);
            Assert.IsTrue(str.Contains("TimeoutError"));
            Assert.IsTrue(str.Contains("timed out"));
        }

        [Test]
        public void TestEngineResultIsNull_WhenNotEngineError()
        {
            var error = ClientError.FromTimeout();
            Assert.IsNull(error.EngineResultCode);
        }

        [Test]
        public void TestMultipleErrorTypes()
        {
            var errors = new[]
            {
                ClientError.FromCancellation(),
                ClientError.FromTimeout(),
                ClientError.FromEngineResult(EngineResult.ILLEGAL_ARGUMENT)
            };

            Assert.AreEqual(3, errors.Length);
            Assert.IsTrue(errors[0].IsCancellation);
            Assert.IsTrue(errors[1].IsTimeout);
            Assert.IsTrue(errors[2].IsEngineError);
        }
    }
}
