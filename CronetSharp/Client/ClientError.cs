using System;
using CronetSharp.Cronet;

namespace CronetSharp.Client
{
    /// <summary>
    /// Represents errors that can occur when using the high-level CronetClient API.
    /// </summary>
    public class ClientError : Exception
    {
        /// <summary>
        /// Gets the type of client error.
        /// </summary>
        public ClientErrorType ErrorType { get; }

        /// <summary>
        /// Gets the underlying Cronet exception, if applicable.
        /// </summary>
        public CronetException CronetError { get; }

        /// <summary>
        /// Gets the engine result code, if applicable.
        /// </summary>
        public EngineResult? EngineResultCode { get; }

        private ClientError(ClientErrorType errorType, string message, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// Creates a ClientError for a Cronet error.
        /// </summary>
        /// <param name="cronetError">The underlying Cronet exception.</param>
        /// <returns>A ClientError wrapping the Cronet error.</returns>
        public static ClientError FromCronetError(CronetException cronetError)
        {
            var error = new ClientError(
                ClientErrorType.CronetError,
                cronetError.CronetErrorMessage ?? "Cronet error occurred",
                cronetError);
            return error;
        }

        /// <summary>
        /// Creates a ClientError for a cancellation.
        /// </summary>
        /// <returns>A ClientError indicating the request was cancelled.</returns>
        public static ClientError FromCancellation()
        {
            return new ClientError(
                ClientErrorType.CancellationError,
                "Request was cancelled");
        }

        /// <summary>
        /// Creates a ClientError for an unexpected engine result.
        /// </summary>
        /// <param name="result">The unexpected engine result.</param>
        /// <returns>A ClientError indicating an unexpected engine result.</returns>
        public static ClientError FromEngineResult(EngineResult result)
        {
            var error = new ClientError(
                ClientErrorType.EngineError,
                $"Unexpected engine result: {result}")
            {
                EngineResultCode = result
            };
            return error;
        }

        /// <summary>
        /// Creates a ClientError for a timeout.
        /// </summary>
        /// <returns>A ClientError indicating the request timed out.</returns>
        public static ClientError FromTimeout()
        {
            return new ClientError(
                ClientErrorType.TimeoutError,
                "Request timed out");
        }

        /// <summary>
        /// Determines whether this error is a Cronet error.
        /// </summary>
        public bool IsCronetError => ErrorType == ClientErrorType.CronetError;

        /// <summary>
        /// Determines whether this error is a cancellation.
        /// </summary>
        public bool IsCancellation => ErrorType == ClientErrorType.CancellationError;

        /// <summary>
        /// Determines whether this error is an engine error.
        /// </summary>
        public bool IsEngineError => ErrorType == ClientErrorType.EngineError;

        /// <summary>
        /// Determines whether this error is a timeout.
        /// </summary>
        public bool IsTimeout => ErrorType == ClientErrorType.TimeoutError;

        public override string ToString()
        {
            return $"{ErrorType}: {Message}";
        }
    }

    /// <summary>
    /// Defines the types of errors that can occur in the CronetClient.
    /// </summary>
    public enum ClientErrorType
    {
        /// <summary>
        /// Internal Cronet error.
        /// </summary>
        CronetError,

        /// <summary>
        /// The request was cancelled.
        /// </summary>
        CancellationError,

        /// <summary>
        /// Unexpected Cronet engine result.
        /// </summary>
        EngineError,

        /// <summary>
        /// The request timed out.
        /// </summary>
        TimeoutError
    }
}
