using ConScript.Core;

namespace ConScript.Interfaces
{
    /// <summary>
    /// Interface for configuration providers
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        /// Loads configuration from the specified file
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <param name="password">Optional password for encrypted files</param>
        /// <returns>Result of the operation</returns>
        OperationResult Load(string filePath, string password = null);

        /// <summary>
        /// Saves configuration to the specified file
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        /// <returns>Result of the operation</returns>
        OperationResult Save(string filePath);

        /// <summary>
        /// Gets a value by its key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <param name="defaultValue">Default value if the key is not found</param>
        /// <returns>Value of the specified key or default value</returns>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Sets a value by its key
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">Key of the value</param>
        /// <param name="value">Value to set</param>
        /// <returns>Configuration builder for fluent API</returns>
        IConfigurationBuilder Set<T>(string key, T value);

        /// <summary>
        /// Gets a value by its path (e.g. "player.stats.health")
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="path">Path to the value</param>
        /// <param name="defaultValue">Default value if the path is not found</param>
        /// <returns>Value at the specified path or default value</returns>
        T GetByPath<T>(string path, T defaultValue = default);
    }
}
