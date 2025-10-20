using System;
using System.Linq;
using CronetSharp;
using CronetSharp.Client;
using NUnit.Framework;

namespace CronetSharp.Tests
{
    [TestFixture]
    public class OrderedRequestTest : SetupCronet
    {
        [Test]
        public void Constructor_WithMethodAndUri_CreatesRequest()
        {
            // Act
            var request = new OrderedRequest("GET", "https://example.com");

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("https://example.com", request.Uri);
            Assert.AreEqual("HTTP/1.1", request.Version);
            Assert.IsNotNull(request.Headers);
            Assert.AreEqual(0, request.Headers.Count);
            Assert.IsNull(request.Body);
        }

        [Test]
        public void Constructor_WithMethodUriAndBody_CreatesRequest()
        {
            // Arrange
            var body = Body.FromString("test data");

            // Act
            var request = new OrderedRequest("POST", "https://example.com", body);

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("https://example.com", request.Uri);
            Assert.AreEqual(body, request.Body);
        }

        [Test]
        public void Constructor_WithNullMethod_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrderedRequest(null, "https://example.com"));
        }

        [Test]
        public void Constructor_WithEmptyMethod_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrderedRequest("", "https://example.com"));
        }

        [Test]
        public void Constructor_WithNullUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrderedRequest("GET", null));
        }

        [Test]
        public void Constructor_WithEmptyUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrderedRequest("GET", ""));
        }

        [Test]
        public void AddHeader_AddsHeaderToList()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act
            var result = request.AddHeader("X-Custom-Header", "value");

            // Assert
            Assert.AreEqual(request, result); // Check method chaining
            Assert.AreEqual(1, request.Headers.Count);
            Assert.AreEqual("X-Custom-Header", request.Headers[0].Name);
            Assert.AreEqual("value", request.Headers[0].Value);
        }

        [Test]
        public void AddHeader_PreservesInsertionOrder()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act
            request.AddHeader("X-First", "1")
                   .AddHeader("X-Second", "2")
                   .AddHeader("X-Third", "3");

            // Assert
            Assert.AreEqual(3, request.Headers.Count);
            Assert.AreEqual("X-First", request.Headers[0].Name);
            Assert.AreEqual("X-Second", request.Headers[1].Name);
            Assert.AreEqual("X-Third", request.Headers[2].Name);
        }

        [Test]
        public void AddHeader_AllowsDuplicateHeaderNames()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act
            request.AddHeader("Content-Type", "application/json")
                   .AddHeader("X-Other", "value")
                   .AddHeader("Content-Type", "application/json; charset=utf-8");

            // Assert
            Assert.AreEqual(3, request.Headers.Count);
            Assert.AreEqual("Content-Type", request.Headers[0].Name);
            Assert.AreEqual("application/json", request.Headers[0].Value);
            Assert.AreEqual("X-Other", request.Headers[1].Name);
            Assert.AreEqual("Content-Type", request.Headers[2].Name);
            Assert.AreEqual("application/json; charset=utf-8", request.Headers[2].Value);
        }

        [Test]
        public void AddHeader_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => request.AddHeader(null, "value"));
        }

        [Test]
        public void AddHeader_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => request.AddHeader("X-Header", null));
        }

        [Test]
        public void SetVersion_UpdatesVersion()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act
            var result = request.SetVersion("HTTP/2");

            // Assert
            Assert.AreEqual(request, result); // Check method chaining
            Assert.AreEqual("HTTP/2", request.Version);
        }

        [Test]
        public void SetVersion_WithNullVersion_ThrowsArgumentNullException()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => request.SetVersion(null));
        }

        [Test]
        public void ToUrlRequestParams_CreatesValidParams()
        {
            // Arrange
            var request = new OrderedRequest("POST", "https://example.com");
            request.AddHeader("Content-Type", "application/json")
                   .AddHeader("Authorization", "Bearer token");

            // Act
            var parameters = request.ToUrlRequestParams();

            // Assert
            Assert.IsNotNull(parameters);
            Assert.AreEqual("POST", parameters.GetHttpMethod());
            Assert.AreEqual(2, parameters.GetAllHeaders().Count);
        }

        [Test]
        public void ToUrlRequestParams_PreservesHeaderOrder()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");
            request.AddHeader("X-First", "1")
                   .AddHeader("X-Second", "2")
                   .AddHeader("X-First", "1-duplicate")
                   .AddHeader("X-Third", "3");

            // Act
            var parameters = request.ToUrlRequestParams();

            // Assert
            var headers = parameters.GetAllHeaders();
            Assert.AreEqual(4, headers.Count);
            Assert.AreEqual("X-First", headers[0].GetName());
            Assert.AreEqual("1", headers[0].GetValue());
            Assert.AreEqual("X-Second", headers[1].GetName());
            Assert.AreEqual("2", headers[1].GetValue());
            Assert.AreEqual("X-First", headers[2].GetName());
            Assert.AreEqual("1-duplicate", headers[2].GetValue());
            Assert.AreEqual("X-Third", headers[3].GetName());
            Assert.AreEqual("3", headers[3].GetValue());
        }

        [Test]
        public void ToUrlRequestParams_WithBody_IncludesUploadProvider()
        {
            // Arrange
            var body = Body.FromString("test data");
            var request = new OrderedRequest("POST", "https://example.com", body);

            // Act
            var parameters = request.ToUrlRequestParams();

            // Assert
            Assert.IsNotNull(parameters);
            // Note: We can't directly test the upload provider without executor,
            // but we can verify the method doesn't throw
        }

        [Test]
        public void ToUrlRequestParams_WithoutBody_DoesNotIncludeUploadProvider()
        {
            // Arrange
            var request = new OrderedRequest("GET", "https://example.com");

            // Act
            var parameters = request.ToUrlRequestParams();

            // Assert
            Assert.IsNotNull(parameters);
            Assert.AreEqual("GET", parameters.GetHttpMethod());
        }
    }

    [TestFixture]
    public class OrderedRequestBuilderTest
    {
        [Test]
        public void Constructor_CreatesBuilderWithDefaults()
        {
            // Act
            var builder = new OrderedRequestBuilder();
            var request = builder.Build();

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("/", request.Uri);
            Assert.AreEqual("HTTP/1.1", request.Version);
            Assert.AreEqual(0, request.Headers.Count);
            Assert.IsNull(request.Body);
        }

        [Test]
        public void Method_SetsMethod()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();

            // Act
            var request = builder.Method("POST").Build();

            // Assert
            Assert.AreEqual("POST", request.Method);
        }

        [Test]
        public void Uri_SetsUri()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();

            // Act
            var request = builder.Uri("https://api.example.com").Build();

            // Assert
            Assert.AreEqual("https://api.example.com", request.Uri);
        }

        [Test]
        public void Version_SetsVersion()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();

            // Act
            var request = builder.Version("HTTP/2").Build();

            // Assert
            Assert.AreEqual("HTTP/2", request.Version);
        }

        [Test]
        public void Header_AddsHeader()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();

            // Act
            var request = builder.Header("X-Custom", "value").Build();

            // Assert
            Assert.AreEqual(1, request.Headers.Count);
            Assert.AreEqual("X-Custom", request.Headers[0].Name);
            Assert.AreEqual("value", request.Headers[0].Value);
        }

        [Test]
        public void Header_PreservesOrder()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();

            // Act
            var request = builder
                .Header("X-First", "1")
                .Header("X-Second", "2")
                .Header("X-Third", "3")
                .Build();

            // Assert
            Assert.AreEqual(3, request.Headers.Count);
            Assert.AreEqual("X-First", request.Headers[0].Name);
            Assert.AreEqual("X-Second", request.Headers[1].Name);
            Assert.AreEqual("X-Third", request.Headers[2].Name);
        }

        [Test]
        public void Body_SetsBody()
        {
            // Arrange
            var builder = new OrderedRequestBuilder();
            var body = Body.FromString("test");

            // Act
            var request = builder.Body(body).Build();

            // Assert
            Assert.AreEqual(body, request.Body);
        }

        [Test]
        public void FluentApi_AllowsMethodChaining()
        {
            // Act
            var request = new OrderedRequestBuilder()
                .Method("POST")
                .Uri("https://api.example.com/data")
                .Version("HTTP/2")
                .Header("Content-Type", "application/json")
                .Header("Authorization", "Bearer token")
                .Body(Body.FromString("test data"))
                .Build();

            // Assert
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("https://api.example.com/data", request.Uri);
            Assert.AreEqual("HTTP/2", request.Version);
            Assert.AreEqual(2, request.Headers.Count);
            Assert.IsNotNull(request.Body);
        }

        [Test]
        public void Build_WithInvalidUri_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Uri("not a valid uri !!!");

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithNullMethod_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Method(null);

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithEmptyMethod_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Method("");

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithNullUri_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Uri(null);

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithNullVersion_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Version(null);

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithNullHeaderName_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Header(null, "value");

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_WithNullHeaderValue_ThrowsBuilderException()
        {
            // Arrange
            var builder = new OrderedRequestBuilder().Header("X-Header", null);

            // Act & Assert
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_AfterError_ContinuesReturningBuilder()
        {
            // Arrange
            var builder = new OrderedRequestBuilder()
                .Method(null) // This causes an error
                .Uri("https://example.com"); // This should still return the builder

            // Act & Assert
            Assert.IsNotNull(builder);
            Assert.Throws<BuilderException>(() => builder.Build());
        }

        [Test]
        public void Build_StopsProcessingAfterFirstError()
        {
            // Arrange
            var builder = new OrderedRequestBuilder()
                .Uri("not valid") // This causes an error
                .Method("POST"); // This should be ignored

            // Act & Assert
            var ex = Assert.Throws<BuilderException>(() => builder.Build());
            Assert.IsTrue(ex.Message.Contains("Invalid URI"));
        }
    }

    [TestFixture]
    public class OrderedRequestFactoryTest
    {
        [Test]
        public void Builder_ReturnsNewBuilder()
        {
            // Act
            var builder = OrderedRequestFactory.Builder();

            // Assert
            Assert.IsNotNull(builder);
            Assert.IsInstanceOf<OrderedRequestBuilder>(builder);
        }

        [Test]
        public void Builder_MultipleCalls_ReturnsDifferentInstances()
        {
            // Act
            var builder1 = OrderedRequestFactory.Builder();
            var builder2 = OrderedRequestFactory.Builder();

            // Assert
            Assert.AreNotSame(builder1, builder2);
        }

        [Test]
        public void Builder_CanBuildCompleteRequest()
        {
            // Act
            var request = OrderedRequestFactory.Builder()
                .Method("POST")
                .Uri("https://api.example.com")
                .Header("Content-Type", "application/json")
                .Body(Body.FromString("{}"))
                .Build();

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("https://api.example.com", request.Uri);
            Assert.AreEqual(1, request.Headers.Count);
            Assert.IsNotNull(request.Body);
        }
    }
}
