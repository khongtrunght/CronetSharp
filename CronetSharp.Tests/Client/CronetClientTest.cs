using System;
using System.Threading;
using System.Threading.Tasks;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    [TestFixture]
    public class CronetClientTest
    {
        [Test]
        public void Constructor_Default_CreatesClient()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                Assert.IsNotNull(client);
                Assert.AreEqual(TimeSpan.FromSeconds(30), client.DefaultTimeout);
                Assert.IsNotNull(client.ShouldRedirect);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Constructor_WithCustomParams_CreatesClient()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                var engineParams = new CronetEngineParams
                {
                    UserAgent = "TestClient/1.0",
                    EnableHttp2 = true
                };

                client = new CronetClient(engineParams);

                Assert.IsNotNull(client);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Constructor_WithNullParams_ThrowsArgumentNullException()
        {
            SetupCronet.Setup();
            Assert.Throws<ArgumentNullException>(() => new CronetClient(null));
        }

        [Test]
        public void DefaultTimeout_CanBeSet()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                client.DefaultTimeout = TimeSpan.FromSeconds(10);
                Assert.AreEqual(TimeSpan.FromSeconds(10), client.DefaultTimeout);

                client.DefaultTimeout = null;
                Assert.IsNull(client.DefaultTimeout);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void ShouldRedirect_CanBeSet()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                Func<string, bool> customRedirect = url => url.Contains("example.com");
                client.ShouldRedirect = customRedirect;

                Assert.AreEqual(customRedirect, client.ShouldRedirect);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void ShouldRedirect_SetToNull_UsesDefaultBehavior()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                client.ShouldRedirect = null;

                Assert.IsNotNull(client.ShouldRedirect);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Send_WithNullUrl_ThrowsArgumentNullException()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                Assert.Throws<ArgumentNullException>(() => client.Send(null));
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Send_WithEmptyUrl_ThrowsArgumentNullException()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                Assert.Throws<ArgumentNullException>(() => client.Send(""));
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public async Task SendAsync_WithNullUrl_ThrowsArgumentNullException()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await client.SendAsync(null));
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Get_WithValidUrl_ReturnsMethodName()
        {
            // This is a basic test - full integration tests are in E2E tests
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                // We can't test actual HTTP calls without a real server,
                // so we just verify the method exists and signature is correct
                Assert.DoesNotThrow(() =>
                {
                    var methodInfo = typeof(CronetClient).GetMethod("Get");
                    Assert.IsNotNull(methodInfo);
                    Assert.AreEqual(typeof(HttpResponse), methodInfo.ReturnType);
                });
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Post_WithValidUrl_HasCorrectSignature()
        {
            // This is a basic test - full integration tests are in E2E tests
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                // Verify the method exists and signature is correct
                var methodInfo = typeof(CronetClient).GetMethod("Post");
                Assert.IsNotNull(methodInfo);
                Assert.AreEqual(typeof(HttpResponse), methodInfo.ReturnType);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public async Task GetAsync_HasCorrectSignature()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                // Verify the method exists and signature is correct
                var methodInfo = typeof(CronetClient).GetMethod("GetAsync");
                Assert.IsNotNull(methodInfo);
                Assert.AreEqual(typeof(Task<HttpResponse>), methodInfo.ReturnType);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public async Task PostAsync_HasCorrectSignature()
        {
            CronetClient client = null;
            try
            {
                SetupCronet.Setup();
                client = new CronetClient();

                // Verify the method exists and signature is correct
                var methodInfo = typeof(CronetClient).GetMethod("PostAsync");
                Assert.IsNotNull(methodInfo);
                Assert.AreEqual(typeof(Task<HttpResponse>), methodInfo.ReturnType);
            }
            finally
            {
                client?.Dispose();
            }
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            SetupCronet.Setup();
            var client = new CronetClient();

            Assert.DoesNotThrow(() => client.Dispose());
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        public void Send_AfterDispose_ThrowsObjectDisposedException()
        {
            SetupCronet.Setup();
            var client = new CronetClient();
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => client.Send("https://example.com"));
        }

        [Test]
        public async Task SendAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            SetupCronet.Setup();
            var client = new CronetClient();
            client.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await client.SendAsync("https://example.com"));
        }

        // Note: Actual HTTP request/response tests are in the E2E test suite
        // These unit tests focus on API surface validation and parameter checking
    }
}
