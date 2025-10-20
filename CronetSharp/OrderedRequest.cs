using System;
using System.Collections.Generic;
using CronetSharp.Client;

namespace CronetSharp
{
    /// <summary>
    /// Exception thrown when building an OrderedRequest fails.
    /// </summary>
    public class BuilderException : Exception
    {
        public BuilderException(string message) : base(message) { }
        public BuilderException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// A request builder that preserves header insertion order.
    ///
    /// Unlike standard HTTP request builders, this class maintains headers in a List
    /// which preserves the exact order headers are inserted, even when the same header
    /// name appears multiple times separated by other headers.
    /// </summary>
    public class OrderedRequest
    {
        /// <summary>
        /// Gets the HTTP method for the request.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets the URI for the request.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets the HTTP version for the request (e.g., "HTTP/1.1", "HTTP/2").
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets the ordered list of headers for the request.
        /// Each header is stored as a key-value pair, preserving insertion order.
        /// </summary>
        public List<(string Name, string Value)> Headers { get; }

        /// <summary>
        /// Gets the request body.
        /// </summary>
        public Body Body { get; set; }

        /// <summary>
        /// Creates a new OrderedRequest with the given method and URI.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., "GET", "POST").</param>
        /// <param name="uri">The request URI.</param>
        public OrderedRequest(string method, string uri)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentNullException(nameof(uri));

            Method = method;
            Uri = uri;
            Version = "HTTP/1.1";
            Headers = new List<(string, string)>();
            Body = null;
        }

        /// <summary>
        /// Creates a new OrderedRequest with the given method, URI, and body.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., "GET", "POST").</param>
        /// <param name="uri">The request URI.</param>
        /// <param name="body">The request body.</param>
        public OrderedRequest(string method, string uri, Body body)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentNullException(nameof(uri));

            Method = method;
            Uri = uri;
            Version = "HTTP/1.1";
            Headers = new List<(string, string)>();
            Body = body;
        }

        /// <summary>
        /// Adds a header to the request, preserving insertion order.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <returns>This OrderedRequest instance for method chaining.</returns>
        public OrderedRequest AddHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Headers.Add((name, value));
            return this;
        }

        /// <summary>
        /// Sets the HTTP version for the request.
        /// </summary>
        /// <param name="version">The HTTP version (e.g., "HTTP/1.1", "HTTP/2").</param>
        /// <returns>This OrderedRequest instance for method chaining.</returns>
        public OrderedRequest SetVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException(nameof(version));

            Version = version;
            return this;
        }

        /// <summary>
        /// Converts this OrderedRequest to a UrlRequestParams instance.
        /// </summary>
        /// <param name="executor">Optional executor for upload data processing.</param>
        /// <returns>A UrlRequestParams configured with the request settings.</returns>
        public UrlRequestParams ToUrlRequestParams(Executor executor = null)
        {
            var requestParams = new UrlRequestParams();
            requestParams.SetHttpMethod(Method);

            // Add headers in the exact order they were inserted
            foreach (var (name, value) in Headers)
            {
                var header = new HttpHeader();
                header.SetName(name);
                header.SetValue(value);
                requestParams.AddHeader(header);
            }

            // Add body if present
            if (Body != null && Body.Length.HasValue && Body.Length.Value > 0)
            {
                var uploadProvider = new BodyUploadProvider(Body);
                if (executor != null)
                {
                    requestParams.SetUploadDataProvider(uploadProvider.Provider, executor);
                }
                else
                {
                    // If no executor provided, just set the provider without executor
                    requestParams.SetUploadDataProvider(uploadProvider.Provider, null);
                }
            }

            return requestParams;
        }
    }

    /// <summary>
    /// A builder for constructing OrderedRequest instances with a fluent API.
    /// </summary>
    public class OrderedRequestBuilder
    {
        private string _method;
        private string _uri;
        private string _version;
        private List<(string Name, string Value)> _headers;
        private Body _body;
        private Exception _error;

        /// <summary>
        /// Creates a new OrderedRequestBuilder with default values.
        /// </summary>
        public OrderedRequestBuilder()
        {
            _method = "GET";
            _uri = "/";
            _version = "HTTP/1.1";
            _headers = new List<(string, string)>();
            _body = null;
            _error = null;
        }

        /// <summary>
        /// Sets the HTTP method for the request.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., "GET", "POST", "PUT", "DELETE").</param>
        /// <returns>This builder instance for method chaining.</returns>
        public OrderedRequestBuilder Method(string method)
        {
            if (_error != null)
                return this;

            if (string.IsNullOrWhiteSpace(method))
            {
                _error = new BuilderException("Method cannot be null or empty");
                return this;
            }

            _method = method;
            return this;
        }

        /// <summary>
        /// Sets the URI for the request.
        /// </summary>
        /// <param name="uri">The request URI.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public OrderedRequestBuilder Uri(string uri)
        {
            if (_error != null)
                return this;

            if (string.IsNullOrWhiteSpace(uri))
            {
                _error = new BuilderException("URI cannot be null or empty");
                return this;
            }

            // Basic URI validation
            if (!System.Uri.TryCreate(uri, UriKind.Absolute, out _) &&
                !System.Uri.TryCreate(uri, UriKind.Relative, out _))
            {
                _error = new BuilderException($"Invalid URI: {uri}");
                return this;
            }

            _uri = uri;
            return this;
        }

        /// <summary>
        /// Sets the HTTP version for the request.
        /// </summary>
        /// <param name="version">The HTTP version (e.g., "HTTP/1.1", "HTTP/2").</param>
        /// <returns>This builder instance for method chaining.</returns>
        public OrderedRequestBuilder Version(string version)
        {
            if (_error != null)
                return this;

            if (string.IsNullOrWhiteSpace(version))
            {
                _error = new BuilderException("Version cannot be null or empty");
                return this;
            }

            _version = version;
            return this;
        }

        /// <summary>
        /// Adds a header to the request, preserving insertion order.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public OrderedRequestBuilder Header(string name, string value)
        {
            if (_error != null)
                return this;

            if (string.IsNullOrWhiteSpace(name))
            {
                _error = new BuilderException("Header name cannot be null or empty");
                return this;
            }

            if (value == null)
            {
                _error = new BuilderException("Header value cannot be null");
                return this;
            }

            _headers.Add((name, value));
            return this;
        }

        /// <summary>
        /// Sets the request body.
        /// </summary>
        /// <param name="body">The request body.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public OrderedRequestBuilder Body(Body body)
        {
            if (_error != null)
                return this;

            _body = body;
            return this;
        }

        /// <summary>
        /// Builds the OrderedRequest instance.
        /// </summary>
        /// <returns>A configured OrderedRequest.</returns>
        /// <exception cref="BuilderException">Thrown if the builder encountered any errors.</exception>
        public OrderedRequest Build()
        {
            if (_error != null)
            {
                throw new BuilderException("Failed to build OrderedRequest", _error);
            }

            var request = new OrderedRequest(_method, _uri, _body);
            request.Version = _version;

            foreach (var (name, value) in _headers)
            {
                request.Headers.Add((name, value));
            }

            return request;
        }
    }

    /// <summary>
    /// Factory class for creating OrderedRequest builders.
    /// </summary>
    public static class OrderedRequestFactory
    {
        /// <summary>
        /// Creates a new OrderedRequestBuilder instance.
        /// </summary>
        /// <returns>A new OrderedRequestBuilder.</returns>
        public static OrderedRequestBuilder Builder()
        {
            return new OrderedRequestBuilder();
        }
    }
}
