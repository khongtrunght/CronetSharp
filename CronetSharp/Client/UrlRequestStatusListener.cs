using System;
using System.Runtime.InteropServices;
using CronetSharp.Cronet;

namespace CronetSharp.Client
{
    /// <summary>
    /// High-level wrapper for monitoring the status of a UrlRequest.
    /// Provides real-time notifications about request lifecycle events.
    /// </summary>
    public sealed class UrlRequestStatusListener : IDisposable
    {
        private IntPtr _nativePtr;
        private GCHandle _callbackHandle;
        private readonly Action<UrlRequestStatus> _onStatus;
        private bool _disposed;

        /// <summary>
        /// Creates a new status listener with the specified callback.
        /// </summary>
        /// <param name="onStatus">Callback invoked when the request status changes.
        /// The callback receives the new status as a parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown when onStatus is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native listener creation fails.</exception>
        public UrlRequestStatusListener(Action<UrlRequestStatus> onStatus)
        {
            _onStatus = onStatus ?? throw new ArgumentNullException(nameof(onStatus));

            // Create delegate that will be called from native code
            Cronet.UrlRequestStatusListener.OnStatusFunc nativeCallback = OnStatusThunk;

            // Pin the delegate to prevent garbage collection
            _callbackHandle = GCHandle.Alloc(nativeCallback);

            // Create the native listener
            _nativePtr = Cronet.UrlRequestStatusListener.Cronet_UrlRequestStatusListener_CreateWith(nativeCallback);

            if (_nativePtr == IntPtr.Zero)
            {
                if (_callbackHandle.IsAllocated)
                    _callbackHandle.Free();
                throw new InvalidOperationException("Failed to create native UrlRequestStatusListener");
            }
        }

        /// <summary>
        /// Gets the native pointer to the underlying Cronet status listener.
        /// </summary>
        internal IntPtr NativePtr
        {
            get
            {
                ThrowIfDisposed();
                return _nativePtr;
            }
        }

        /// <summary>
        /// Thunk method called from native code. Translates native callback to managed callback.
        /// </summary>
        private void OnStatusThunk(IntPtr listenerPtr, UrlRequestStatus status)
        {
            try
            {
                // Invoke the user's callback with the status
                _onStatus?.Invoke(status);
            }
            catch (Exception ex)
            {
                // Prevent exceptions from crossing the native boundary
                // In production, you might want to log this
                System.Diagnostics.Debug.WriteLine($"Exception in status listener callback: {ex}");
            }
        }

        /// <summary>
        /// Manually trigger a status notification. This can be used for testing
        /// or to simulate status changes.
        /// </summary>
        /// <param name="status">The status to report.</param>
        public void OnStatus(UrlRequestStatus status)
        {
            ThrowIfDisposed();
            Cronet.UrlRequestStatusListener.Cronet_UrlRequestStatusListener_OnStatus(_nativePtr, status);
        }

        /// <summary>
        /// Releases all resources used by this status listener.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_nativePtr != IntPtr.Zero)
            {
                Cronet.UrlRequestStatusListener.Cronet_UrlRequestStatusListener_Destroy(_nativePtr);
                _nativePtr = IntPtr.Zero;
            }

            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to ensure native resources are cleaned up.
        /// </summary>
        ~UrlRequestStatusListener()
        {
            Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UrlRequestStatusListener));
        }
    }

    /// <summary>
    /// Extension methods for UrlRequest to support status monitoring.
    /// </summary>
    public static class UrlRequestStatusExtensions
    {
        /// <summary>
        /// Attaches a status listener to monitor this request's lifecycle.
        /// </summary>
        /// <param name="request">The request to monitor.</param>
        /// <param name="listener">The status listener.</param>
        /// <remarks>
        /// The listener must remain alive for the duration of the request.
        /// Consider storing it in a field or variable that won't be garbage collected.
        /// </remarks>
        public static void AttachStatusListener(this Cronet.UrlRequest request, UrlRequestStatusListener listener)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            // This would require additional native bindings to actually attach the listener
            // For now, this is a placeholder for the API design
            // In the Rust code, this is done during request creation
            throw new NotImplementedException(
                "Status listener attachment requires additional native bindings. " +
                "Currently, status listeners must be attached during request creation.");
        }
    }

    /// <summary>
    /// Provides human-readable descriptions for UrlRequestStatus values.
    /// </summary>
    public static class UrlRequestStatusDescriptions
    {
        /// <summary>
        /// Gets a human-readable description of the given status.
        /// </summary>
        /// <param name="status">The status to describe.</param>
        /// <returns>A description of the status.</returns>
        public static string GetDescription(UrlRequestStatus status)
        {
            return status switch
            {
                UrlRequestStatus.Invalid =>
                    "The request is completed, canceled, or not started",
                UrlRequestStatus.Idle =>
                    "The request has not yet begun or is waiting for the consumer",
                UrlRequestStatus.WaitingForStalledSocketPool =>
                    "Waiting for a socket from a stalled pool",
                UrlRequestStatus.WaitingForAvailableSocket =>
                    "Waiting for an available socket from the pool",
                UrlRequestStatus.WaitingForDelegate =>
                    "The delegate has chosen to block this request",
                UrlRequestStatus.WaitingForCache =>
                    "Waiting for access to a cache resource",
                UrlRequestStatus.DownloadingPacFile =>
                    "Downloading the PAC (Proxy Auto-Config) script",
                UrlRequestStatus.ResolvingProxyForUrl =>
                    "Waiting for proxy autoconfig script to return a proxy",
                UrlRequestStatus.ResolvingHostInPacFile =>
                    "Resolving host name in proxy autoconfig script",
                UrlRequestStatus.EstablishingProxyTunnel =>
                    "Establishing a tunnel through the proxy server",
                UrlRequestStatus.ResolvingHost =>
                    "Resolving the host name",
                UrlRequestStatus.Connecting =>
                    "Establishing a TCP/network connection",
                UrlRequestStatus.SslHandshake =>
                    "Performing SSL/TLS handshake",
                UrlRequestStatus.SendingRequest =>
                    "Uploading request data to the server",
                UrlRequestStatus.WaitingForResponse =>
                    "Waiting for response headers from the server",
                UrlRequestStatus.ReadingResponse =>
                    "Reading response body from the server",
                _ => $"Unknown status: {(int)status}"
            };
        }

        /// <summary>
        /// Determines if the given status represents an active/in-progress state.
        /// </summary>
        /// <param name="status">The status to check.</param>
        /// <returns>True if the request is actively being processed, false otherwise.</returns>
        public static bool IsActive(UrlRequestStatus status)
        {
            return status switch
            {
                UrlRequestStatus.Invalid => false,
                UrlRequestStatus.Idle => false,
                _ => true
            };
        }

        /// <summary>
        /// Determines if the given status represents a network activity state.
        /// </summary>
        /// <param name="status">The status to check.</param>
        /// <returns>True if the status involves network I/O, false otherwise.</returns>
        public static bool IsNetworkActive(UrlRequestStatus status)
        {
            return status switch
            {
                UrlRequestStatus.Connecting => true,
                UrlRequestStatus.SslHandshake => true,
                UrlRequestStatus.SendingRequest => true,
                UrlRequestStatus.WaitingForResponse => true,
                UrlRequestStatus.ReadingResponse => true,
                _ => false
            };
        }
    }
}
