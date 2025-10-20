using System;
using System.Threading;
using System.Threading.Tasks;

namespace CronetSharp.Client
{
    /// <summary>
    /// High-level HTTP client wrapper around Cronet that provides a simplified API
    /// similar to HttpClient for making HTTP requests.
    /// </summary>
    public class CronetClient : IDisposable
    {
        private readonly CronetEngine _engine;
        private readonly Executor _executor;
        private Func<string, bool> _shouldRedirect;
        private TimeSpan? _defaultTimeout;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the default timeout for requests.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan? DefaultTimeout
        {
            get => _defaultTimeout;
            set => _defaultTimeout = value;
        }

        /// <summary>
        /// Gets or sets the function that determines whether a redirect should be followed.
        /// Default follows all redirects.
        /// </summary>
        public Func<string, bool> ShouldRedirect
        {
            get => _shouldRedirect;
            set => _shouldRedirect = value ?? (url => true);
        }

        /// <summary>
        /// Creates a new CronetClient with default settings.
        /// Enables HTTP/2, QUIC, and Brotli compression.
        /// </summary>
        public CronetClient()
        {
            var engineParams = new CronetEngineParams
            {
                EnableHttp2 = true,
                EnableQuic = true,
                EnableBrotli = true,
                EnableCheckResult = false
            };

            _engine = new CronetEngine(engineParams);
            var result = _engine.Start();

            if (result != EngineResult.Success)
            {
                throw new InvalidOperationException($"Failed to start Cronet engine: {result}");
            }

            _executor = Executors.NewSingleThreadExecutor();
            _shouldRedirect = url => true;
            _defaultTimeout = TimeSpan.FromSeconds(30);
            _disposed = false;
        }

        /// <summary>
        /// Creates a new CronetClient with custom engine parameters.
        /// This allows configuring proxy settings, user agent, and other engine options.
        /// </summary>
        /// <param name="engineParams">The engine parameters to use.</param>
        public CronetClient(CronetEngineParams engineParams)
        {
            if (engineParams == null)
            {
                throw new ArgumentNullException(nameof(engineParams));
            }

            _engine = new CronetEngine(engineParams);
            var result = _engine.Start();

            if (result != EngineResult.Success)
            {
                throw new InvalidOperationException($"Failed to start Cronet engine: {result}");
            }

            _executor = Executors.NewSingleThreadExecutor();
            _shouldRedirect = url => true;
            _defaultTimeout = TimeSpan.FromSeconds(30);
            _disposed = false;
        }

        /// <summary>
        /// Sends an HTTP request synchronously.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <param name="method">The HTTP method (default: GET).</param>
        /// <param name="body">The request body (optional).</param>
        /// <param name="headers">Additional headers (optional).</param>
        /// <returns>The HTTP response.</returns>
        /// <exception cref="ClientError">Thrown when the request fails.</exception>
        public HttpResponse Send(
            string url,
            string method = "GET",
            Body body = null,
            params (string name, string value)[] headers)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            ThrowIfDisposed();

            return SendInternal(url, method, body, headers);
        }

        /// <summary>
        /// Sends an HTTP request asynchronously.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <param name="method">The HTTP method (default: GET).</param>
        /// <param name="body">The request body (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="headers">Additional headers (optional).</param>
        /// <returns>A task representing the HTTP response.</returns>
        /// <exception cref="ClientError">Thrown when the request fails.</exception>
        public async Task<HttpResponse> SendAsync(
            string url,
            string method = "GET",
            Body body = null,
            CancellationToken cancellationToken = default,
            params (string name, string value)[] headers)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            ThrowIfDisposed();

            return await Task.Run(() => SendInternal(url, method, body, headers), cancellationToken);
        }

        /// <summary>
        /// Sends a GET request.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <returns>The HTTP response.</returns>
        public HttpResponse Get(string url)
        {
            return Send(url, "GET");
        }

        /// <summary>
        /// Sends a GET request asynchronously.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the HTTP response.</returns>
        public Task<HttpResponse> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            return SendAsync(url, "GET", null, cancellationToken);
        }

        /// <summary>
        /// Sends a POST request.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <param name="body">The request body.</param>
        /// <returns>The HTTP response.</returns>
        public HttpResponse Post(string url, Body body)
        {
            return Send(url, "POST", body);
        }

        /// <summary>
        /// Sends a POST request asynchronously.
        /// </summary>
        /// <param name="url">The URL to request.</param>
        /// <param name="body">The request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the HTTP response.</returns>
        public Task<HttpResponse> PostAsync(string url, Body body, CancellationToken cancellationToken = default)
        {
            return SendAsync(url, "POST", body, cancellationToken);
        }

        private HttpResponse SendInternal(
            string url,
            string method,
            Body body,
            (string name, string value)[] headers)
        {
            // Create request parameters
            var requestParams = new UrlRequestParams();
            requestParams.SetHttpMethod(method);

            // Add headers
            if (headers != null)
            {
                foreach (var (name, value) in headers)
                {
                    var header = new HttpHeader();
                    header.SetName(name);
                    header.SetValue(value);
                    requestParams.AddHeader(header);
                }
            }

            // Add body if present
            if (body != null && body.Length.HasValue && body.Length.Value > 0)
            {
                var uploadProvider = new BodyUploadProvider(body);
                requestParams.SetUploadDataProvider(uploadProvider.Provider, _executor);
            }

            requestParams.SetUploadDataExecutor(_executor);

            // Create response handler
            var handler = new ResponseHandler(_shouldRedirect);

            // Create URL request
            var urlRequest = _engine.NewUrlRequest(url, handler.Callback, _executor, requestParams);

            // Start the request
            var startResult = urlRequest.Start();
            if (startResult != EngineResult.Success)
            {
                throw ClientError.FromEngineResult(startResult);
            }

            // Wait for response with timeout
            (ResponseStatus status, HttpResponse response, ClientError error) result;

            if (_defaultTimeout.HasValue)
            {
                // Wait with timeout
                var timeoutTask = Task.Delay(_defaultTimeout.Value);
                var responseTask = handler.Task;

                var completedTask = Task.WhenAny(responseTask, timeoutTask).Result;

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    urlRequest.Cancel();

                    // Wait briefly for cancellation to complete
                    var cancelTimeout = Task.Delay(TimeSpan.FromSeconds(1));
                    Task.WhenAny(handler.Task, cancelTimeout).Wait();

                    throw ClientError.FromTimeout();
                }

                result = responseTask.Result;
            }
            else
            {
                // Wait indefinitely
                result = handler.Task.Result;
            }

            // Process result
            switch (result.status)
            {
                case ResponseStatus.Success:
                    return result.response;

                case ResponseStatus.Canceled:
                    throw ClientError.FromCancellation();

                case ResponseStatus.Error:
                    throw result.error;

                default:
                    throw new InvalidOperationException($"Unknown response status: {result.status}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CronetClient));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                // Try to shutdown the engine gracefully
                _engine?.Shutdown();
            }
            catch
            {
                // Ignore shutdown errors - engine will clean up on dispose
            }
            finally
            {
                _engine?.Dispose();
                _executor?.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
