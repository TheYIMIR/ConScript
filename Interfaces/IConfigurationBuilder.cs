namespace ConScript.Interfaces
{
    /// <summary>
    /// Interface for configuration builders (fluent API)
    /// </summary>
    public interface IConfigurationBuilder
    {
        /// <summary>
        /// Adds a comment to the current configuration entry
        /// </summary>
        /// <param name="comment">Comment text</param>
        /// <returns>Configuration builder for chaining</returns>
        IConfigurationBuilder WithComment(string comment);

        /// <summary>
        /// Sets validation rule for the current configuration entry
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="validator">Validation function</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>Configuration builder for chaining</returns>
        IConfigurationBuilder WithValidation<T>(Func<T, bool> validator, string errorMessage);
    }
}
