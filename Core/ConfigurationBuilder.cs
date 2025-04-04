using ConScript.Interfaces;

namespace ConScript.Core
{
    /// <summary>
    /// Configuration builder implementation
    /// </summary>
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly ConScript _config;
        private readonly string _key;

        /// <summary>
        /// Creates a new configuration builder
        /// </summary>
        public ConfigurationBuilder(ConScript config, string key)
        {
            _config = config;
            _key = key;
        }

        /// <inheritdoc />
        public IConfigurationBuilder WithComment(string comment)
        {
            _config.SetComment(_key, comment);
            return this;
        }

        /// <inheritdoc />
        public IConfigurationBuilder WithValidation<T>(Func<T, bool> validator, string errorMessage)
        {
            _config.SetValidation(_key, obj => obj is T typedObj && validator(typedObj), errorMessage);
            return this;
        }
    }
}
