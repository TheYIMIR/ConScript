namespace ConScript.Models
{
    /// <summary>
    /// Validation rule for configuration values
    /// </summary>
    public class ValidationRule
    {
        /// <summary>
        /// Validation function
        /// </summary>
        public Func<object, bool> Validator { get; }

        /// <summary>
        /// Error message if validation fails
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Creates a new validation rule
        /// </summary>
        public ValidationRule(Func<object, bool> validator, string errorMessage)
        {
            Validator = validator;
            ErrorMessage = errorMessage;
        }
    }
}
