using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests.Client
{
    /// <summary>
    /// End-to-end integration tests for CronetClient.
    /// These tests make real HTTP requests to public APIs to validate the complete request/response flow.
    /// </summary>
    [TestFixture]
    public class CronetClientE2ETest : SetupCronet
    {
        private CronetClient _client;

        [SetUp]
        public new void Setup()
        {
            base.Setup();
            _client = new CronetClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        /// <summary>
        /// Test: Simple GET request to a public API.
        /// Validates that the client can make a basic HTTP GET request and receive a valid response.
        /// </summary>
        [Test]
        public void Get_SimpleRequest_ReturnsSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/get";

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsNotNull(response.Body);
            Assert.IsTrue(response.Body.Length > 0);

            // Verify we got a valid JSON response
            var bodyText = Encoding.UTF8.GetString(response.Body);
            Assert.IsTrue(bodyText.Contains("httpbin.org"));
        }

        /// <summary>
        /// Test: Async GET request.
        /// Validates async/await pattern works correctly.
        /// </summary>
        [Test]
        public async Task GetAsync_SimpleRequest_ReturnsSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/get";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsNotNull(response.Body);
            Assert.IsTrue(response.Body.Length > 0);
        }

        /// <summary>
        /// Test: POST request with JSON body.
        /// Validates that the client can send POST requests with body data.
        /// </summary>
        [Test]
        public void Post_WithJsonBody_ReturnsSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/post";
            var jsonBody = "{\"name\":\"test\",\"value\":\"123\"}";
            var body = Body.FromString(jsonBody);

            // Act
            var response = _client.Send(
                url,
                "POST",
                body,
                ("Content-Type", "application/json")
            );

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);

            var responseBody = Encoding.UTF8.GetString(response.Body);
            Assert.IsTrue(responseBody.Contains("test"));
            Assert.IsTrue(responseBody.Contains("123"));
        }

        /// <summary>
        /// Test: POST request with byte array body.
        /// Validates binary data upload.
        /// </summary>
        [Test]
        public void Post_WithBytesBody_ReturnsSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/post";
            var data = Encoding.UTF8.GetBytes("test data");
            var body = Body.FromBytes(data);

            // Act
            var response = _client.Post(url, body);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);

            var responseBody = Encoding.UTF8.GetString(response.Body);
            Assert.IsTrue(responseBody.Contains("test data"));
        }

        /// <summary>
        /// Test: Async POST request.
        /// Validates async POST operations.
        /// </summary>
        [Test]
        public async Task PostAsync_WithBody_ReturnsSuccess()
        {
            // Arrange
            var url = "https://httpbin.org/post";
            var body = Body.FromString("async test");

            // Act
            var response = await _client.PostAsync(url, body);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
        }

        /// <summary>
        /// Test: Request with custom headers.
        /// Validates that custom headers are sent correctly.
        /// </summary>
        [Test]
        public void Get_WithCustomHeaders_SendsHeaders()
        {
            // Arrange
            var url = "https://httpbin.org/headers";

            // Act
            var response = _client.Send(
                url,
                "GET",
                null,
                ("X-Custom-Header", "test-value"),
                ("X-Another-Header", "another-value")
            );

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);

            var responseBody = Encoding.UTF8.GetString(response.Body);
            Assert.IsTrue(responseBody.Contains("X-Custom-Header"));
            Assert.IsTrue(responseBody.Contains("test-value"));
        }

        /// <summary>
        /// Test: Timeout handling.
        /// Validates that requests timeout correctly when server is slow.
        /// </summary>
        [Test]
        public void Get_WithTimeout_ThrowsTimeoutError()
        {
            // Arrange
            var url = "https://httpbin.org/delay/10"; // Server delays 10 seconds
            _client.DefaultTimeout = TimeSpan.FromSeconds(2); // Client timeout is 2 seconds

            // Act & Assert
            var ex = Assert.Throws<ClientError>(() => _client.Get(url));
            Assert.IsTrue(ex.IsTimeoutError);
        }

        /// <summary>
        /// Test: Timeout does not occur for fast requests.
        /// Validates that timeout only triggers for actually slow requests.
        /// </summary>
        [Test]
        public void Get_WithSufficientTimeout_Succeeds()
        {
            // Arrange
            var url = "https://httpbin.org/delay/1"; // Server delays 1 second
            _client.DefaultTimeout = TimeSpan.FromSeconds(5); // Client timeout is 5 seconds

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
        }

        /// <summary>
        /// Test: Redirect following (default behavior).
        /// Validates that redirects are followed automatically.
        /// </summary>
        [Test]
        public void Get_WithRedirect_FollowsAutomatically()
        {
            // Arrange
            var url = "https://httpbin.org/redirect/2"; // Redirects 2 times

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode); // Should end up at final destination
        }

        /// <summary>
        /// Test: Custom redirect handler.
        /// Validates that custom redirect logic can be implemented.
        /// </summary>
        [Test]
        public void Get_WithCustomRedirectHandler_UsesCustomLogic()
        {
            // Arrange
            var url = "https://httpbin.org/redirect-to?url=https://httpbin.org/get";
            var redirectFollowed = false;

            _client.ShouldRedirect = redirectUrl =>
            {
                redirectFollowed = true;
                return true; // Allow redirect
            };

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsTrue(redirectFollowed, "Custom redirect handler should have been called");
        }

        /// <summary>
        /// Test: Blocking redirects with custom handler.
        /// Validates that redirects can be blocked by returning false.
        /// </summary>
        [Test]
        public void Get_WithRedirectBlocked_DoesNotFollow()
        {
            // Arrange
            var url = "https://httpbin.org/redirect-to?url=https://httpbin.org/get";

            _client.ShouldRedirect = redirectUrl => false; // Block all redirects

            // Act
            // When redirect is blocked, the response will likely be a 3xx status code
            // or an error depending on Cronet's behavior
            try
            {
                var response = _client.Get(url);
                // If we get a response, it should be a redirect status code
                Assert.IsTrue(response.StatusCode >= 300 && response.StatusCode < 400);
            }
            catch (ClientError ex)
            {
                // Some implementations may throw an error when redirect is blocked
                // This is acceptable behavior
                Assert.IsNotNull(ex);
            }
        }

        /// <summary>
        /// Test: Error handling for invalid URL.
        /// Validates that invalid URLs are handled gracefully.
        /// </summary>
        [Test]
        public void Get_WithInvalidUrl_ThrowsError()
        {
            // Arrange
            var url = "https://this-domain-does-not-exist-12345678.com";

            // Act & Assert
            Assert.Throws<ClientError>(() => _client.Get(url));
        }

        /// <summary>
        /// Test: Error handling for DNS failure.
        /// Validates that DNS resolution errors are reported correctly.
        /// </summary>
        [Test]
        public void Get_WithDnsFailure_ThrowsCronetError()
        {
            // Arrange
            var url = "https://nonexistent-domain-12345678.invalid";

            // Act & Assert
            var ex = Assert.Throws<ClientError>(() => _client.Get(url));
            Assert.IsTrue(ex.IsCronetError);
        }

        /// <summary>
        /// Test: Cancellation via CancellationToken.
        /// Validates that async requests can be cancelled.
        /// </summary>
        [Test]
        public async Task GetAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var url = "https://httpbin.org/delay/5"; // Slow endpoint
            var cts = new CancellationTokenSource();

            // Act
            var task = _client.GetAsync(url, cts.Token);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500)); // Cancel after 500ms

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }

        /// <summary>
        /// Test: Multiple sequential requests.
        /// Validates that the client can handle multiple requests in sequence.
        /// </summary>
        [Test]
        public void Get_MultipleRequests_AllSucceed()
        {
            // Arrange
            var url = "https://httpbin.org/get";

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                var response = _client.Get(url);
                Assert.IsNotNull(response);
                Assert.AreEqual(200, response.StatusCode);
            }
        }

        /// <summary>
        /// Test: Response headers are accessible.
        /// Validates that response headers can be read.
        /// </summary>
        [Test]
        public void Get_ResponseHeaders_AreAccessible()
        {
            // Arrange
            var url = "https://httpbin.org/response-headers?X-Test-Header=test-value";

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Headers);
            Assert.IsTrue(response.Headers.Count > 0);
        }

        /// <summary>
        /// Test: Different HTTP methods.
        /// Validates that various HTTP methods are supported.
        /// </summary>
        [Test]
        public void Send_WithDifferentMethods_Succeed()
        {
            // Test PUT
            var putResponse = _client.Send("https://httpbin.org/put", "PUT", Body.FromString("data"));
            Assert.AreEqual(200, putResponse.StatusCode);

            // Test DELETE
            var deleteResponse = _client.Send("https://httpbin.org/delete", "DELETE");
            Assert.AreEqual(200, deleteResponse.StatusCode);

            // Test PATCH
            var patchResponse = _client.Send("https://httpbin.org/patch", "PATCH", Body.FromString("data"));
            Assert.AreEqual(200, patchResponse.StatusCode);
        }

        /// <summary>
        /// Test: Large response body.
        /// Validates that large responses are handled correctly.
        /// </summary>
        [Test]
        public void Get_LargeResponse_HandlesCorrectly()
        {
            // Arrange
            var url = "https://httpbin.org/bytes/100000"; // 100KB response

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual(100000, response.Body.Length);
        }

        /// <summary>
        /// Test: HTTPS connection.
        /// Validates that HTTPS connections work correctly.
        /// </summary>
        [Test]
        public void Get_HttpsUrl_Succeeds()
        {
            // Arrange
            var url = "https://httpbin.org/get";

            // Act
            var response = _client.Get(url);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsTrue(response.Url.StartsWith("https://"));
        }

        /// <summary>
        /// Test: User agent can be customized.
        /// Validates that custom user agent is sent in requests.
        /// </summary>
        [Test]
        public void Get_WithCustomUserAgent_SendsCorrectly()
        {
            // Arrange
            var engineParams = new CronetEngineParams
            {
                UserAgent = "CustomAgent/1.0",
                EnableHttp2 = true,
                EnableQuic = true,
                EnableBrotli = true
            };

            using (var customClient = new CronetClient(engineParams))
            {
                var url = "https://httpbin.org/user-agent";

                // Act
                var response = customClient.Get(url);

                // Assert
                Assert.IsNotNull(response);
                Assert.AreEqual(200, response.StatusCode);

                var responseBody = Encoding.UTF8.GetString(response.Body);
                Assert.IsTrue(responseBody.Contains("CustomAgent/1.0"));
            }
        }
    }
}
