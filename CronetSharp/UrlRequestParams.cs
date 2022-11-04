﻿using System;

namespace CronetSharp
{
    public class UrlRequestParams : IDisposable
    {
        public IntPtr Pointer { get; }

        public UrlRequestParams()
        {
            Pointer = Cronet.UrlRequestParams.Cronet_UrlRequestParams_Create();
        }

        public UrlRequestParams(IntPtr urlRequestParamsPtr)
        {
            Pointer = urlRequestParamsPtr;
        }

        public void Dispose()
        {
            ClearHeaders();
            UploadDataProvider?.Dispose();
            UploadDataProviderExecutor?.Dispose();
            RequestFinishedInfoListener?.Dispose();
            RequestFinishedInfoListenerExecutor?.Dispose();

            if (Pointer == IntPtr.Zero)
            {
                return;
            }

            Cronet.UrlRequestParams.Cronet_UrlRequestParams_Destroy(Pointer);
        }

        public void AddHeader(string header, string value)
        {
            var httpHeader = new HttpHeader(header, value);
            Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_headers_add(Pointer, httpHeader.Pointer);
        }

        /// <summary>
        /// Set or get the request headers.
        /// </summary>
        public HttpHeader[] Headers
        {
            set
            {
                foreach (var header in value)
                    Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_headers_add(Pointer, header.Pointer);
            }
            get
            {
                var size = Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_headers_size(Pointer);
                var headers = new HttpHeader[size];
                for (uint i = 0; i < size; i++)
                {
                    var header = new HttpHeader(Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_headers_at(Pointer, i));
                    headers[i] = header;
                }
                return headers;
            }
        }

        /// <summary>
        /// Marks that the executors this request will use to notify callbacks (for UploadDataProviders and UrlRequest.Callbacks) is intentionally performing inline execution.
        /// </summary>
        public bool AllowDirectExecutor
        {
            get => Cronet.UrlRequestParams.Cronet_UrlRequestParams_allow_direct_executor_get(Pointer);
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_allow_direct_executor_set(Pointer, value);
        }

        /// <summary>
        /// Disables cache for the request.
        /// </summary>
        public bool DisableCache
        {
            get => Cronet.UrlRequestParams.Cronet_UrlRequestParams_disable_cache_get(Pointer);
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_disable_cache_set(Pointer, value);
        }

        /// <summary>
        /// Sets the HTTP method verb to use for this request.
        /// The default when this method is not called is "GET" if the request has no body or "POST" if it does.
        /// Supported methods: "GET", "HEAD", "DELETE", "POST" or "PUT".
        /// </summary>
        public string HttpMethod
        {
            get => Cronet.UrlRequestParams.Cronet_UrlRequestParams_http_method_get(Pointer);
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_http_method_set(Pointer, value);
        }

        public Proxy Proxy
        {
            get => Proxy.TryParse(Cronet.UrlRequestParams.Cronet_UrlRequestParams_proxy_get(Pointer));
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_proxy_set(Pointer, value.Format(ProxyFormat.ReverseNotation));
        }

        /// <summary>
        /// Sets priority of the request.
        /// </summary>
        public Cronet.RequestPriority Priority
        {
            get => Cronet.UrlRequestParams.Cronet_UrlRequestParams_priority_get(Pointer);
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_priority_set(Pointer, value);
        }

        /// <summary>
        /// Sets upload data provider.
        /// </summary>
        public UploadDataProvider UploadDataProvider
        {
            get => new UploadDataProvider(Cronet.UrlRequestParams.Cronet_UrlRequestParams_upload_data_provider_get(Pointer));
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_upload_data_provider_set(Pointer, value.Pointer);
        }

        /// <summary>
        /// Sets upload data provider engine.
        /// </summary>
        public Executor UploadDataProviderExecutor
        {
            get => new Executor(Cronet.UrlRequestParams.Cronet_UrlRequestParams_upload_data_provider_executor_get(Pointer));
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_upload_data_provider_executor_set(Pointer, value.Pointer);
        }

        /// <summary>
        /// Sets idempotency
        /// </summary>
        public Cronet.Idempotency Idempotency
        {
            get => Cronet.UrlRequestParams.Cronet_UrlRequestParams_idempotency_get(Pointer);
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_idempotency_set(Pointer, value);
        }

        /// <summary>
        /// Set request finished info listener.
        /// </summary>
        public RequestFinishedInfoListener RequestFinishedInfoListener
        {
            get => new RequestFinishedInfoListener(Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_finished_listener_get(Pointer));
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_finished_listener_set(Pointer, value.Pointer);
        }

        /// <summary>
        /// Set the executor to use for the request finished info listener.
        /// </summary>
        public Executor RequestFinishedInfoListenerExecutor
        {
            get => new Executor(Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_finished_executor_get(Pointer));
            set => Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_finished_executor_set(Pointer, value.Pointer);
        }

        /// <summary>
        /// Remove all headers
        /// </summary>
        public void ClearHeaders()
        {
            if (Pointer == IntPtr.Zero)
            {
                return;
            }

            Cronet.UrlRequestParams.Cronet_UrlRequestParams_request_headers_clear(Pointer);
        }
    }
}