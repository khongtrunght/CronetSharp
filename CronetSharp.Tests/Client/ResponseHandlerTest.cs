using System;
using System.Threading.Tasks;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class ResponseHandlerTest
    {
        [Test]
        public void Constructor_CreatesCallback()
        {
            using var handler = new ResponseHandler();

            Assert.IsNotNull(handler);
            Assert.IsNotNull(handler.Callback);
            Assert.IsNotNull(handler.Task);
        }

        [Test]
        public void Constructor_WithCustomRedirectFunc_UsesCustomFunc()
        {
            bool customFuncCalled = false;
            Func<string, bool> customRedirect = (url) =>
            {
                customFuncCalled = true;
                return false;
            };

            using var handler = new ResponseHandler(customRedirect);

            Assert.IsNotNull(handler);
            Assert.IsNotNull(handler.Callback);
        }

        [Test]
        public void SetBufferSize_UpdatesBufferSize()
        {
            using var handler = new ResponseHandler();

            // Default is 512
            Assert.DoesNotThrow(() => handler.SetBufferSize(1024));
            Assert.DoesNotThrow(() => handler.SetBufferSize(4096));
        }

        [Test]
        public void Task_IsNotCompleted_Initially()
        {
            using var handler = new ResponseHandler();

            Assert.IsFalse(handler.Task.IsCompleted);
        }

        [Test]
        public void Dispose_DisposesCallback()
        {
            var handler = new ResponseHandler();

            Assert.DoesNotThrow(() => handler.Dispose());

            // Second dispose should not throw
            Assert.DoesNotThrow(() => handler.Dispose());
        }

        [Test]
        public void HttpResponse_DefaultConstructor_InitializesProperties()
        {
            var response = new HttpResponse();

            Assert.IsNotNull(response.Headers);
            Assert.IsNotNull(response.Body);
            Assert.AreEqual(0, response.Headers.Count);
        }

        [Test]
        public void HttpResponse_Properties_CanBeSet()
        {
            var response = new HttpResponse
            {
                StatusCode = 200,
                StatusText = "OK",
                Url = "https://example.com",
                WasCached = false,
                NegotiatedProtocol = "h2"
            };

            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("OK", response.StatusText);
            Assert.AreEqual("https://example.com", response.Url);
            Assert.IsFalse(response.WasCached);
            Assert.AreEqual("h2", response.NegotiatedProtocol);
        }

        [Test]
        public void ResponseStatus_Enum_HasExpectedValues()
        {
            Assert.AreEqual(ResponseStatus.Success, ResponseStatus.Success);
            Assert.AreEqual(ResponseStatus.Canceled, ResponseStatus.Canceled);
            Assert.AreEqual(ResponseStatus.Error, ResponseStatus.Error);

            Assert.AreNotEqual(ResponseStatus.Success, ResponseStatus.Error);
        }

        // Note: Full integration tests with actual request/response flow
        // are deferred to end-to-end tests, as they require a running Cronet engine
        // and real or mocked network requests.
    }
}
