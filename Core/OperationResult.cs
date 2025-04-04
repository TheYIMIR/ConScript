namespace ConScript.Core
{
    /// <summary>
    /// Result of an operation that can either succeed or fail with a message
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Contains error message if the operation failed
        /// </summary>
        public string Message { get; private set; }

        private OperationResult(bool success, string message = null)
        {
            Success = success;
            Message = message;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static OperationResult Succeeded() => new OperationResult(true);

        /// <summary>
        /// Creates a failed result with the specified error message
        /// </summary>
        public static OperationResult Failed(string message) => new OperationResult(false, message);
    }
}
