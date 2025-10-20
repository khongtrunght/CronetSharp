using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CronetSharp.Client;

namespace CronetSharp.Export
{
    /// <summary>
    /// Native C API for CronetSharp interop with unmanaged code.
    /// Provides a C-compatible interface for creating clients, sending requests, and managing memory.
    ///
    /// Thread Safety Note:
    /// - Each client instance should be used by only one thread at a time
    /// - For concurrent requests, create multiple client instances
    /// - Maximum 50 clients should exist concurrently to prevent resource exhaustion
    /// </summary>
    public static class NativeApi
    {
        /// <summary>
        /// Response structure for debugging and inspection.
        /// Contains request and response details for analysis.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct HSProtectDebugResponse
        {
            /// <summary>HTTP status code (200, 404, etc.) or -1 for errors</summary>
            public int StatusCode;

            /// <summary>Pointer to null-terminated response body string</summary>
            public IntPtr ResponseBody;

            /// <summary>Length of response body in bytes</summary>
            public IntPtr ResponseBodyLen;

            /// <summary>Pointer to null-terminated request headers string (sent headers)</summary>
            public IntPtr RequestHeaders;

            /// <summary>Pointer to null-terminated request body string (sent body)</summary>
            public IntPtr RequestBody;

            /// <summary>Pointer to null-terminated response headers string (received headers)</summary>
            public IntPtr ResponseHeaders;
        }

        /// <summary>
        /// Creates a new CronetClient instance with optional proxy configuration.
        /// </summary>
        /// <param name="proxyRules">
        /// Proxy configuration string in format:
        /// - "host:port" for simple proxy
        /// - "host:port:username:password" for authenticated proxy
        /// - "http://user:pass@host:port" for URL format
        /// - "socks5://host:port" for SOCKS5 proxy
        /// - null or empty for no proxy
        /// </param>
        /// <param name="proxyType">Deprecated parameter, kept for compatibility. Can be null.</param>
        /// <returns>Pointer to CronetClient instance or IntPtr.Zero on failure</returns>
        public static IntPtr CreateClient(IntPtr proxyRules, IntPtr proxyType)
        {
            try
            {
                // Check if proxy is provided
                bool useProxy = false;
                string proxyStr = null;

                if (proxyRules != IntPtr.Zero)
                {
                    proxyStr = Marshal.PtrToStringAnsi(proxyRules);
                    useProxy = !string.IsNullOrEmpty(proxyStr);
                }

                CronetClient client;

                if (useProxy)
                {
                    // Parse the proxy string
                    var proxyInfo = Proxy.Parse(proxyStr);

                    if (proxyInfo != null)
                    {
                        // Create engine params with proxy configuration
                        var engineParams = new CronetEngineParams();
                        engineParams.ProxyRules = proxyInfo.ToCronetRules();
                        engineParams.EnableCheckResult = false;

                        // Set proxy authentication if provided
                        if (!string.IsNullOrEmpty(proxyInfo.Username))
                        {
                            engineParams.ProxyUsername = proxyInfo.Username;
                        }
                        if (!string.IsNullOrEmpty(proxyInfo.Password))
                        {
                            engineParams.ProxyPassword = proxyInfo.Password;
                        }

                        client = new CronetClient(engineParams);
                    }
                    else
                    {
                        // If parsing fails, create a regular client without proxy
                        client = new CronetClient();
                    }
                }
                else
                {
                    // Create a regular client without proxy
                    client = new CronetClient();
                }

                // Pin the client in memory and return its pointer
                GCHandle handle = GCHandle.Alloc(client, GCHandleType.Normal);
                return GCHandle.ToIntPtr(handle);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Frees a CronetClient instance created by CreateClient.
        /// </summary>
        /// <param name="client">Pointer to CronetClient instance</param>
        public static void FreeClient(IntPtr client)
        {
            if (client == IntPtr.Zero)
                return;

            try
            {
                GCHandle handle = GCHandle.FromIntPtr(client);
                if (handle.IsAllocated)
                {
                    var clientObj = handle.Target as CronetClient;
                    clientObj?.Dispose();
                    handle.Free();
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        /// <summary>
        /// Sends an HTTP request using the specified client.
        /// </summary>
        /// <param name="client">Pointer to CronetClient instance</param>
        /// <param name="url">Null-terminated URL string</param>
        /// <param name="method">Null-terminated HTTP method (GET, POST, etc.)</param>
        /// <param name="body">Null-terminated request body or IntPtr.Zero</param>
        /// <param name="headers">Null-terminated headers string (format: "Name: Value\nName2: Value2")</param>
        /// <param name="isBodyBase64">1 if body is base64-encoded, 0 otherwise</param>
        /// <param name="isHeadersBase64">1 if headers are base64-encoded, 0 otherwise</param>
        /// <returns>Pointer to HSProtectDebugResponse or IntPtr.Zero on failure</returns>
        public static IntPtr SendRequest(
            IntPtr client,
            IntPtr url,
            IntPtr method,
            IntPtr body,
            IntPtr headers,
            int isBodyBase64,
            int isHeadersBase64)
        {
            if (client == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                GCHandle handle = GCHandle.FromIntPtr(client);
                if (!handle.IsAllocated)
                    return IntPtr.Zero;

                var clientObj = handle.Target as CronetClient;
                if (clientObj == null)
                    return IntPtr.Zero;

                // Parse URL
                if (url == IntPtr.Zero)
                    return IntPtr.Zero;

                string urlStr = Marshal.PtrToStringAnsi(url);
                if (string.IsNullOrEmpty(urlStr))
                    return IntPtr.Zero;

                // Parse method
                string methodStr = "GET";
                if (method != IntPtr.Zero)
                {
                    methodStr = Marshal.PtrToStringAnsi(method);
                    if (string.IsNullOrEmpty(methodStr))
                        methodStr = "GET";
                }

                // Build request using OrderedRequest for header ordering
                var requestBuilder = OrderedRequestFactory.Builder()
                    .Method(methodStr)
                    .Uri(urlStr)
                    .Version("HTTP/1.1");

                string requestHeadersDebug = "";
                string requestBodyDebug = "";

                // Process headers
                if (headers != IntPtr.Zero)
                {
                    string headersStr = Marshal.PtrToStringAnsi(headers);
                    if (!string.IsNullOrEmpty(headersStr))
                    {
                        string headersContent = headersStr;
                        if (isHeadersBase64 != 0)
                        {
                            try
                            {
                                byte[] decoded = Convert.FromBase64String(headersStr);
                                headersContent = Encoding.UTF8.GetString(decoded);
                            }
                            catch
                            {
                                // If decoding fails, use original
                                headersContent = headersStr;
                            }
                        }

                        requestHeadersDebug = headersContent;

                        // Parse headers (format: "Name: Value\nName2: Value2")
                        string[] lines = headersContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in lines)
                        {
                            int colonIndex = line.IndexOf(':');
                            if (colonIndex > 0)
                            {
                                string name = line.Substring(0, colonIndex).Trim();
                                string value = line.Substring(colonIndex + 1).Trim();
                                requestBuilder = requestBuilder.Header(name, value);
                            }
                        }
                    }
                }

                // Process body
                Body bodyContent = Body.Empty;
                if (body != IntPtr.Zero)
                {
                    string bodyStr = Marshal.PtrToStringAnsi(body);
                    if (!string.IsNullOrEmpty(bodyStr))
                    {
                        if (isBodyBase64 != 0)
                        {
                            try
                            {
                                byte[] decoded = Convert.FromBase64String(bodyStr);
                                requestBodyDebug = Encoding.UTF8.GetString(decoded);
                                bodyContent = Body.FromBytes(decoded);
                            }
                            catch
                            {
                                // If decoding fails, use original
                                requestBodyDebug = bodyStr;
                                bodyContent = Body.FromString(bodyStr);
                            }
                        }
                        else
                        {
                            requestBodyDebug = bodyStr;
                            bodyContent = Body.FromString(bodyStr);
                        }
                    }
                }

                // Build the request
                var orderedRequest = requestBuilder.Body(bodyContent).Build();

                // Convert to UrlRequestParams
                var requestParams = orderedRequest.ToUrlRequestParams();

                // Send request using the client
                var response = clientObj.Send(urlStr, requestParams);

                // Build debug response
                var debugResponse = new HSProtectDebugResponse();
                debugResponse.StatusCode = (int)response.StatusCode;

                // Response body
                byte[] responseBodyBytes = response.Body.AsBytes();
                string responseBodyStr = Encoding.UTF8.GetString(responseBodyBytes);
                debugResponse.ResponseBody = Marshal.StringToHGlobalAnsi(responseBodyStr);
                debugResponse.ResponseBodyLen = new IntPtr(responseBodyBytes.Length);

                // Response headers
                var responseHeadersBuilder = new StringBuilder();

                // Add status line
                responseHeadersBuilder.AppendFormat("HTTP/1.1 {0} {1}\n",
                    response.StatusCode,
                    GetStatusCodeDescription(response.StatusCode));

                // Add all headers
                foreach (var header in response.Headers)
                {
                    string headerValue = header.Value ?? "";
                    responseHeadersBuilder.AppendFormat("{0}: {1}\n", header.Name, headerValue);
                }

                string responseHeadersStr = responseHeadersBuilder.ToString();
                debugResponse.ResponseHeaders = Marshal.StringToHGlobalAnsi(responseHeadersStr);

                // Request debug info
                debugResponse.RequestHeaders = Marshal.StringToHGlobalAnsi(requestHeadersDebug);
                debugResponse.RequestBody = Marshal.StringToHGlobalAnsi(requestBodyDebug);

                // Allocate and return the response
                IntPtr responsePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HSProtectDebugResponse)));
                Marshal.StructureToPtr(debugResponse, responsePtr, false);
                return responsePtr;
            }
            catch (Exception ex)
            {
                // Return error response
                try
                {
                    var errorResponse = new HSProtectDebugResponse();
                    errorResponse.StatusCode = -1;
                    errorResponse.ResponseBody = Marshal.StringToHGlobalAnsi($"Request failed: {ex.Message}");
                    errorResponse.ResponseBodyLen = IntPtr.Zero;
                    errorResponse.RequestHeaders = Marshal.StringToHGlobalAnsi("");
                    errorResponse.RequestBody = Marshal.StringToHGlobalAnsi("");
                    errorResponse.ResponseHeaders = Marshal.StringToHGlobalAnsi("");

                    IntPtr responsePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HSProtectDebugResponse)));
                    Marshal.StructureToPtr(errorResponse, responsePtr, false);
                    return responsePtr;
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Frees a response created by SendRequest.
        /// </summary>
        /// <param name="response">Pointer to HSProtectDebugResponse</param>
        public static void FreeResponse(IntPtr response)
        {
            if (response == IntPtr.Zero)
                return;

            try
            {
                var resp = Marshal.PtrToStructure<HSProtectDebugResponse>(response);

                if (resp.ResponseBody != IntPtr.Zero)
                    Marshal.FreeHGlobal(resp.ResponseBody);

                if (resp.RequestHeaders != IntPtr.Zero)
                    Marshal.FreeHGlobal(resp.RequestHeaders);

                if (resp.RequestBody != IntPtr.Zero)
                    Marshal.FreeHGlobal(resp.RequestBody);

                if (resp.ResponseHeaders != IntPtr.Zero)
                    Marshal.FreeHGlobal(resp.ResponseHeaders);

                Marshal.FreeHGlobal(response);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        /// <summary>
        /// Gets a human-readable description for an HTTP status code.
        /// </summary>
        private static string GetStatusCodeDescription(int statusCode)
        {
            switch (statusCode)
            {
                case 200: return "OK";
                case 201: return "Created";
                case 204: return "No Content";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 304: return "Not Modified";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 500: return "Internal Server Error";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                default: return "Unknown";
            }
        }
    }

    /// <summary>
    /// Delegate-based wrapper for exposing NativeApi functions to unmanaged code.
    /// These can be passed to native code via function pointers.
    /// </summary>
    public static class NativeApiDelegates
    {
        /// <summary>
        /// Delegate type for CreateClient function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateClientDelegate(IntPtr proxyRules, IntPtr proxyType);

        /// <summary>
        /// Delegate type for FreeClient function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeClientDelegate(IntPtr client);

        /// <summary>
        /// Delegate type for SendRequest function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr SendRequestDelegate(
            IntPtr client,
            IntPtr url,
            IntPtr method,
            IntPtr body,
            IntPtr headers,
            int isBodyBase64,
            int isHeadersBase64);

        /// <summary>
        /// Delegate type for FreeResponse function.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeResponseDelegate(IntPtr response);

        /// <summary>
        /// Gets a delegate instance for CreateClient that can be passed to unmanaged code.
        /// Keep this delegate alive as long as unmanaged code might call it.
        /// </summary>
        public static CreateClientDelegate GetCreateClientDelegate()
        {
            return new CreateClientDelegate(NativeApi.CreateClient);
        }

        /// <summary>
        /// Gets a delegate instance for FreeClient that can be passed to unmanaged code.
        /// Keep this delegate alive as long as unmanaged code might call it.
        /// </summary>
        public static FreeClientDelegate GetFreeClientDelegate()
        {
            return new FreeClientDelegate(NativeApi.FreeClient);
        }

        /// <summary>
        /// Gets a delegate instance for SendRequest that can be passed to unmanaged code.
        /// Keep this delegate alive as long as unmanaged code might call it.
        /// </summary>
        public static SendRequestDelegate GetSendRequestDelegate()
        {
            return new SendRequestDelegate(NativeApi.SendRequest);
        }

        /// <summary>
        /// Gets a delegate instance for FreeResponse that can be passed to unmanaged code.
        /// Keep this delegate alive as long as unmanaged code might call it.
        /// </summary>
        public static FreeResponseDelegate GetFreeResponseDelegate()
        {
            return new FreeResponseDelegate(NativeApi.FreeResponse);
        }
    }
}
