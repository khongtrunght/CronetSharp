using System;
using System.Runtime.InteropServices;
using System.Text;
using CronetSharp.Export;
using NUnit.Framework;

namespace CronetSharp.Tests.Export
{
    /// <summary>
    /// Unit tests for NativeApi export layer.
    /// Tests C API functionality for interop with unmanaged code.
    /// </summary>
    [TestFixture]
    public class NativeApiTest
    {
        /// <summary>
        /// Helper to convert string to unmanaged pointer.
        /// Must be freed with Marshal.FreeHGlobal.
        /// </summary>
        private IntPtr StringToPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return IntPtr.Zero;
            return Marshal.StringToHGlobalAnsi(str);
        }

        /// <summary>
        /// Helper to convert unmanaged pointer to string.
        /// </summary>
        private string PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(ptr);
        }

        [Test]
        public void CreateClient_WithNullProxy_ReturnsValidClient()
        {
            // Act
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);

            // Assert
            Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

            // Cleanup
            NativeApi.FreeClient(client);
        }

        [Test]
        public void CreateClient_WithEmptyProxy_ReturnsValidClient()
        {
            // Arrange
            IntPtr proxyPtr = StringToPtr("");

            try
            {
                // Act
                IntPtr client = NativeApi.CreateClient(proxyPtr, IntPtr.Zero);

                // Assert
                Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeClient(client);
            }
            finally
            {
                Marshal.FreeHGlobal(proxyPtr);
            }
        }

        [Test]
        public void CreateClient_WithValidProxy_ReturnsValidClient()
        {
            // Arrange
            IntPtr proxyPtr = StringToPtr("proxy.example.com:8080");

            try
            {
                // Act
                IntPtr client = NativeApi.CreateClient(proxyPtr, IntPtr.Zero);

                // Assert
                Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeClient(client);
            }
            finally
            {
                Marshal.FreeHGlobal(proxyPtr);
            }
        }

        [Test]
        public void CreateClient_WithProxyAuth_ReturnsValidClient()
        {
            // Arrange
            IntPtr proxyPtr = StringToPtr("proxy.example.com:8080:user:pass");

            try
            {
                // Act
                IntPtr client = NativeApi.CreateClient(proxyPtr, IntPtr.Zero);

                // Assert
                Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeClient(client);
            }
            finally
            {
                Marshal.FreeHGlobal(proxyPtr);
            }
        }

        [Test]
        public void CreateClient_WithSocks5Proxy_ReturnsValidClient()
        {
            // Arrange
            IntPtr proxyPtr = StringToPtr("socks5://socks.example.com:1080");

            try
            {
                // Act
                IntPtr client = NativeApi.CreateClient(proxyPtr, IntPtr.Zero);

                // Assert
                Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeClient(client);
            }
            finally
            {
                Marshal.FreeHGlobal(proxyPtr);
            }
        }

        [Test]
        public void FreeClient_WithNullClient_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => NativeApi.FreeClient(IntPtr.Zero));
        }

        [Test]
        public void FreeClient_WithValidClient_DisposesClient()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

            // Act & Assert
            Assert.DoesNotThrow(() => NativeApi.FreeClient(client));
        }

        [Test]
        public void FreeClient_CalledTwice_DoesNotThrow()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);

            // Act & Assert
            Assert.DoesNotThrow(() => NativeApi.FreeClient(client));
            Assert.DoesNotThrow(() => NativeApi.FreeClient(client)); // Should handle double-free gracefully
        }

        [Test]
        public void SendRequest_WithNullClient_ReturnsNull()
        {
            // Arrange
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    IntPtr.Zero,
                    urlPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.EqualTo(IntPtr.Zero));
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
            }
        }

        [Test]
        public void SendRequest_WithNullUrl_ReturnsNull()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.EqualTo(IntPtr.Zero));
            }
            finally
            {
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithValidGetRequest_ReturnsResponse()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            IntPtr methodPtr = StringToPtr("GET");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1)); // May fail if network unavailable
                Assert.That(resp.ResponseBody, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithDefaultMethod_UsesGet()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");

            try
            {
                // Act - no method specified, should default to GET
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    IntPtr.Zero, // Null method
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithHeaders_IncludesHeadersInRequest()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            IntPtr methodPtr = StringToPtr("GET");
            IntPtr headersPtr = StringToPtr("User-Agent: test-agent\nAccept: application/json");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    IntPtr.Zero,
                    headersPtr,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1));
                Assert.That(resp.RequestHeaders, Is.Not.EqualTo(IntPtr.Zero));

                string reqHeaders = PtrToString(resp.RequestHeaders);
                Assert.That(reqHeaders, Does.Contain("User-Agent: test-agent"));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                Marshal.FreeHGlobal(headersPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithBody_SendsBodyInRequest()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/post");
            IntPtr methodPtr = StringToPtr("POST");
            IntPtr bodyPtr = StringToPtr("test body content");
            IntPtr headersPtr = StringToPtr("Content-Type: text/plain");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    bodyPtr,
                    headersPtr,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1));
                Assert.That(resp.RequestBody, Is.Not.EqualTo(IntPtr.Zero));

                string reqBody = PtrToString(resp.RequestBody);
                Assert.That(reqBody, Is.EqualTo("test body content"));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                Marshal.FreeHGlobal(bodyPtr);
                Marshal.FreeHGlobal(headersPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithBase64EncodedBody_DecodesBody()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/post");
            IntPtr methodPtr = StringToPtr("POST");

            // "test body content" in base64
            string base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes("test body content"));
            IntPtr bodyPtr = StringToPtr(base64Body);
            IntPtr headersPtr = StringToPtr("Content-Type: text/plain");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    bodyPtr,
                    headersPtr,
                    1, // isBodyBase64 = true
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1));
                Assert.That(resp.RequestBody, Is.Not.EqualTo(IntPtr.Zero));

                string reqBody = PtrToString(resp.RequestBody);
                Assert.That(reqBody, Is.EqualTo("test body content"));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                Marshal.FreeHGlobal(bodyPtr);
                Marshal.FreeHGlobal(headersPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_WithBase64EncodedHeaders_DecodesHeaders()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            IntPtr methodPtr = StringToPtr("GET");

            // "User-Agent: test-agent\nAccept: application/json" in base64
            string headersText = "User-Agent: test-agent\nAccept: application/json";
            string base64Headers = Convert.ToBase64String(Encoding.UTF8.GetBytes(headersText));
            IntPtr headersPtr = StringToPtr(base64Headers);

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    IntPtr.Zero,
                    headersPtr,
                    0,
                    1); // isHeadersBase64 = true

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.StatusCode, Is.EqualTo(200).Or.EqualTo(-1));
                Assert.That(resp.RequestHeaders, Is.Not.EqualTo(IntPtr.Zero));

                string reqHeaders = PtrToString(resp.RequestHeaders);
                Assert.That(reqHeaders, Does.Contain("User-Agent: test-agent"));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                Marshal.FreeHGlobal(headersPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_ResponseContainsResponseHeaders()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            IntPtr methodPtr = StringToPtr("GET");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.ResponseHeaders, Is.Not.EqualTo(IntPtr.Zero));

                string respHeaders = PtrToString(resp.ResponseHeaders);
                Assert.That(respHeaders, Is.Not.Empty);
                Assert.That(respHeaders, Does.Contain("HTTP/1.1")); // Status line

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void SendRequest_ResponseContainsResponseBody()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            IntPtr methodPtr = StringToPtr("GET");

            try
            {
                // Act
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    methodPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                var resp = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(response);
                Assert.That(resp.ResponseBody, Is.Not.EqualTo(IntPtr.Zero));

                string respBody = PtrToString(resp.ResponseBody);
                Assert.That(respBody, Is.Not.Empty);

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                Marshal.FreeHGlobal(methodPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void FreeResponse_WithNullResponse_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => NativeApi.FreeResponse(IntPtr.Zero));
        }

        [Test]
        public void FreeResponse_WithValidResponse_FreesMemory()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");

            try
            {
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                // Act & Assert
                Assert.DoesNotThrow(() => NativeApi.FreeResponse(response));
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void HSProtectDebugResponse_StructLayout_IsCorrect()
        {
            // This test verifies the struct can be marshaled correctly
            var response = new NativeApi.HSProtectDebugResponse
            {
                StatusCode = 200,
                ResponseBody = StringToPtr("test body"),
                ResponseBodyLen = new IntPtr(9),
                RequestHeaders = StringToPtr("headers"),
                RequestBody = StringToPtr("request"),
                ResponseHeaders = StringToPtr("response headers")
            };

            try
            {
                // Allocate and marshal
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeApi.HSProtectDebugResponse)));
                Marshal.StructureToPtr(response, ptr, false);

                // Unmarshal and verify
                var unmarshaled = Marshal.PtrToStructure<NativeApi.HSProtectDebugResponse>(ptr);

                Assert.That(unmarshaled.StatusCode, Is.EqualTo(200));
                Assert.That(PtrToString(unmarshaled.ResponseBody), Is.EqualTo("test body"));
                Assert.That(unmarshaled.ResponseBodyLen, Is.EqualTo(new IntPtr(9)));

                Marshal.FreeHGlobal(ptr);
            }
            finally
            {
                if (response.ResponseBody != IntPtr.Zero)
                    Marshal.FreeHGlobal(response.ResponseBody);
                if (response.RequestHeaders != IntPtr.Zero)
                    Marshal.FreeHGlobal(response.RequestHeaders);
                if (response.RequestBody != IntPtr.Zero)
                    Marshal.FreeHGlobal(response.RequestBody);
                if (response.ResponseHeaders != IntPtr.Zero)
                    Marshal.FreeHGlobal(response.ResponseHeaders);
            }
        }

        [Test]
        public void NativeApiDelegates_CreateClientDelegate_CanBeInvoked()
        {
            // Arrange
            var del = NativeApiDelegates.GetCreateClientDelegate();

            // Act
            IntPtr client = del(IntPtr.Zero, IntPtr.Zero);

            // Assert
            Assert.That(client, Is.Not.EqualTo(IntPtr.Zero));

            // Cleanup
            NativeApi.FreeClient(client);
        }

        [Test]
        public void NativeApiDelegates_FreeClientDelegate_CanBeInvoked()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            var del = NativeApiDelegates.GetFreeClientDelegate();

            // Act & Assert
            Assert.DoesNotThrow(() => del(client));
        }

        [Test]
        public void NativeApiDelegates_SendRequestDelegate_CanBeInvoked()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");
            var del = NativeApiDelegates.GetSendRequestDelegate();

            try
            {
                // Act
                IntPtr response = del(
                    client,
                    urlPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                // Assert
                Assert.That(response, Is.Not.EqualTo(IntPtr.Zero));

                // Cleanup
                NativeApi.FreeResponse(response);
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                NativeApi.FreeClient(client);
            }
        }

        [Test]
        public void NativeApiDelegates_FreeResponseDelegate_CanBeInvoked()
        {
            // Arrange
            IntPtr client = NativeApi.CreateClient(IntPtr.Zero, IntPtr.Zero);
            IntPtr urlPtr = StringToPtr("https://httpbin.org/get");

            try
            {
                IntPtr response = NativeApi.SendRequest(
                    client,
                    urlPtr,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    0);

                var del = NativeApiDelegates.GetFreeResponseDelegate();

                // Act & Assert
                Assert.DoesNotThrow(() => del(response));
            }
            finally
            {
                Marshal.FreeHGlobal(urlPtr);
                NativeApi.FreeClient(client);
            }
        }
    }
}
