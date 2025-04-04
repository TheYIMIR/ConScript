using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConScript
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

    /// <summary>
    /// Interface for encryption services
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the given plain text
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password to use for encryption</param>
        /// <returns>Encrypted text</returns>
        string Encrypt(string plainText, string password);

        /// <summary>
        /// Decrypts the given encrypted text
        /// </summary>
        /// <param name="encryptedText">Text to decrypt</param>
        /// <param name="password">Password to use for decryption</param>
        /// <returns>Result containing the decrypted text or error message</returns>
        (OperationResult Result, string DecryptedText) Decrypt(string encryptedText, string password);
    }

    /// <summary>
    /// AES implementation of the encryption service
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int Iterations = 10000;

        public string Encrypt(string plainText, string password)
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;

            // Generate random salt and IV
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            // Derive key from password using PBKDF2
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();

            // Write salt and IV to the beginning of the output
            memoryStream.Write(salt, 0, salt.Length);
            memoryStream.Write(iv, 0, iv.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public (OperationResult Result, string DecryptedText) Decrypt(string encryptedText, string password)
        {
            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);

                if (cipherTextBytes.Length < 32) // Salt (16) + IV (16)
                {
                    return (OperationResult.Failed("Invalid encrypted data format"), null);
                }

                using var aes = Aes.Create();
                aes.KeySize = KeySize;

                // Extract salt and IV from the cipher text
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];

                Buffer.BlockCopy(cipherTextBytes, 0, salt, 0, 16);
                Buffer.BlockCopy(cipherTextBytes, 16, iv, 0, 16);

                // Derive key from password using PBKDF2
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(cipherTextBytes, 32, cipherTextBytes.Length - 32);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return (OperationResult.Succeeded(), streamReader.ReadToEnd());
            }
            catch (CryptographicException ex)
            {
                return (OperationResult.Failed($"Decryption error: {ex.Message}"), null);
            }
            catch (FormatException ex)
            {
                return (OperationResult.Failed($"Invalid format: {ex.Message}"), null);
            }
            catch (Exception ex)
            {
                return (OperationResult.Failed($"Error during decryption: {ex.Message}"), null);
            }
        }
    }

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

    /// <summary>
    /// Configuration value change event arguments
    /// </summary>
    public class ConfigValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Key of the changed value
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Old value
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// New value
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Creates a new instance of ConfigValueChangedEventArgs
        /// </summary>
        public ConfigValueChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

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

    /// <summary>
    /// Main ConScript class for processing configuration files
    /// </summary>
    public class ConScript : IConfigurationProvider
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<string, object> _configValues = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, string> _comments = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, ValidationRule> _validationRules = new ConcurrentDictionary<string, ValidationRule>();

        private string _encryptionKey = null;
        private readonly IEncryptionService _encryptionService;

        // Compiled regex patterns for better performance
        private static readonly Regex EntryPattern = new Regex(@"(\w+)\s+(\w+)\s*=\s*(.*?);", RegexOptions.Compiled);
        private static readonly Regex BlockPattern = new Regex(@"(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex TypedBlockPattern = new Regex(@"(\w+)\s+(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex ArrayPattern = new Regex(@"(\w+)\s*\[\]\s*=\s*\[(.*?)\];", RegexOptions.Compiled);
        private static readonly Regex CommentPattern = new Regex(@"//(.*)", RegexOptions.Compiled);

        /// <summary>
        /// Event raised when a configuration value changes
        /// </summary>
        public event EventHandler<ConfigValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Creates a new instance of ConScript
        /// </summary>
        public ConScript() : this(new AesEncryptionService()) { }

        /// <summary>
        /// Creates a new instance of ConScript with a custom encryption service
        /// </summary>
        public ConScript(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        /// <inheritdoc />
        public OperationResult Load(string filePath, string password = null)
        {
            if (!File.Exists(filePath))
            {
                return OperationResult.Failed($"File not found: {filePath}");
            }

            try
            {
                _lock.EnterWriteLock();

                _configValues.Clear();
                _comments.Clear();
                _validationRules.Clear();
                _encryptionKey = password;

                string content = File.ReadAllText(filePath);

                // Decrypt if password is provided
                if (password != null)
                {
                    var (result, decryptedText) = _encryptionService.Decrypt(content, password);
                    if (!result.Success)
                    {
                        return result;
                    }
                    content = decryptedText;
                }

                string[] lines = content.Split('\n');
                Stack<ConcurrentDictionary<string, object>> stack = new Stack<ConcurrentDictionary<string, object>>();
                ConcurrentDictionary<string, object> currentBlock = _configValues;
                string currentPath = "";
                string currentBlockName = null;

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    // Process comments
                    var commentMatch = CommentPattern.Match(trimmed);
                    if (commentMatch.Success)
                    {
                        if (!string.IsNullOrEmpty(currentPath))
                        {
                            _comments[currentPath] = commentMatch.Groups[1].Value.Trim();
                        }
                        continue;
                    }

                    // Process typed blocks (e.g. "float playerStats {")
                    var typedBlockMatch = TypedBlockPattern.Match(trimmed);
                    if (typedBlockMatch.Success)
                    {
                        string type = typedBlockMatch.Groups[1].Value;
                        currentBlockName = typedBlockMatch.Groups[2].Value;

                        string newPath = string.IsNullOrEmpty(currentPath)
                            ? currentBlockName
                            : $"{currentPath}.{currentBlockName}";

                        stack.Push(currentBlock);
                        currentBlock = new ConcurrentDictionary<string, object>();
                        currentPath = newPath;
                        continue;
                    }

                    // Process unnamed blocks (e.g. "player {")
                    var blockMatch = BlockPattern.Match(trimmed);
                    if (blockMatch.Success)
                    {
                        currentBlockName = blockMatch.Groups[1].Value;

                        string newPath = string.IsNullOrEmpty(currentPath)
                            ? currentBlockName
                            : $"{currentPath}.{currentBlockName}";

                        stack.Push(currentBlock);
                        currentBlock = new ConcurrentDictionary<string, object>();
                        currentPath = newPath;
                        continue;
                    }

                    // Process block end
                    if (trimmed == "};")
                    {
                        if (stack.Count > 0)
                        {
                            var parent = stack.Pop();
                            if (!string.IsNullOrEmpty(currentBlockName))
                            {
                                parent[currentBlockName] = currentBlock;
                            }

                            currentBlock = parent;

                            // Update path
                            int lastDotIndex = currentPath.LastIndexOf('.');
                            currentPath = lastDotIndex > 0 ? currentPath.Substring(0, lastDotIndex) : "";
                        }
                        continue;
                    }

                    // Process arrays (e.g. "items[] = [1, 2, 3];")
                    var arrayMatch = ArrayPattern.Match(trimmed);
                    if (arrayMatch.Success)
                    {
                        string key = arrayMatch.Groups[1].Value;
                        string[] values = arrayMatch.Groups[2].Value.Split(", ", StringSplitOptions.RemoveEmptyEntries);

                        // Store raw string array for now, will be converted when accessed
                        currentBlock[key] = values;
                        continue;
                    }

                    // Process regular entries (e.g. "int maxHealth = 100;")
                    var entryMatch = EntryPattern.Match(trimmed);
                    if (entryMatch.Success)
                    {
                        string type = entryMatch.Groups[1].Value;
                        string key = entryMatch.Groups[2].Value;
                        string value = entryMatch.Groups[3].Value;

                        // Parse and store the value
                        currentBlock[key] = ParseValue(type, value);
                    }
                }

                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed($"Error loading configuration: {ex.Message}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public OperationResult Save(string filePath)
        {
            try
            {
                _lock.EnterReadLock();

                // StringBuilder anstelle von StringWriter, aber ohne using
                var writer = new StringBuilder();
                foreach (var entry in _configValues)
                {
                    WriteEntry(writer, entry.Key, entry.Value, 0, "");
                }

                string content = writer.ToString();

                // Encrypt if a key is set
                if (_encryptionKey != null)
                {
                    content = _encryptionService.Encrypt(content, _encryptionKey);
                }

                File.WriteAllText(filePath, content);
                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed($"Error saving configuration: {ex.Message}");
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Sets a comment for a configuration key
        /// </summary>
        /// <param name="key">Key to add comment to</param>
        /// <param name="comment">Comment text</param>
        internal void SetComment(string key, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                _comments[key] = comment;
            }
        }

        /// <summary>
        /// Sets a validation rule for a configuration key
        /// </summary>
        /// <param name="key">Key to add validation to</param>
        /// <param name="validator">Validation function</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        internal void SetValidation(string key, Func<object, bool> validator, string errorMessage)
        {
            _validationRules[key] = new ValidationRule(validator, errorMessage);
        }

        private void WriteEntry(StringBuilder writer, string key, object value, int indent, string path)
        {
            string indentSpace = new string(' ', indent * 4);
            string fullPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";

            // Write comment if exists
            if (_comments.TryGetValue(fullPath, out var comment))
            {
                writer.AppendLine($"{indentSpace}// {comment}");
            }

            // Handle different value types
            if (value is ConcurrentDictionary<string, object> block)
            {
                // Write block opening
                writer.AppendLine($"{indentSpace}{key} {{");

                // Write block contents
                foreach (var subEntry in block)
                {
                    WriteEntry(writer, subEntry.Key, subEntry.Value, indent + 1, fullPath);
                }

                // Write block closing
                writer.AppendLine($"{indentSpace}}};");
            }
            else if (value is IEnumerable enumerable && value is not string)
            {
                // Write array
                var items = string.Join(", ", enumerable.Cast<object>());
                writer.AppendLine($"{indentSpace}{key}[] = [{items}];");
            }
            else
            {
                // Write simple entry
                string type = GetTypeString(value);
                string valueStr = value is string ? $"\"{value}\"" : value?.ToString() ?? "null";
                writer.AppendLine($"{indentSpace}{type} {key} = {valueStr};");
            }
        }

        /// <summary>
        /// Gets an integer value
        /// </summary>
        public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);

        /// <summary>
        /// Gets a float value
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f) => Get(key, defaultValue);

        /// <summary>
        /// Gets a double value
        /// </summary>
        public double GetDouble(string key, double defaultValue = 0.0) => Get(key, defaultValue);

        /// <summary>
        /// Gets a string value
        /// </summary>
        public string GetString(string key, string defaultValue = "") => Get(key, defaultValue);

        /// <summary>
        /// Gets a boolean value
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);

        /// <summary>
        /// Gets a date time value
        /// </summary>
        public DateTime GetDateTime(string key, DateTime defaultValue = default) => Get(key, defaultValue);

        /// <summary>
        /// Gets a list of values
        /// </summary>
        public List<T> GetList<T>(string key, List<T> defaultValue = null)
        {
            try
            {
                _lock.EnterReadLock();

                if (_configValues.TryGetValue(key, out var value) && value is object[] array)
                {
                    try
                    {
                        return array.Select(item => ConvertValue<T>(item)).ToList();
                    }
                    catch
                    {
                        return defaultValue ?? new List<T>();
                    }
                }

                return defaultValue ?? new List<T>();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public T Get<T>(string key, T defaultValue = default)
        {
            try
            {
                _lock.EnterReadLock();

                if (_configValues.TryGetValue(key, out var value))
                {
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }

                    // Try to convert the value to the requested type
                    try
                    {
                        return ConvertValue<T>(value);
                    }
                    catch
                    {
                        // Conversion failed, return default
                        return defaultValue;
                    }
                }

                return defaultValue;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IConfigurationBuilder Set<T>(string key, T value)
        {
            try
            {
                _lock.EnterWriteLock();

                // Check validation rules if exist
                if (_validationRules.TryGetValue(key, out var rule))
                {
                    if (!rule.Validator(value))
                    {
                        throw new ArgumentException(rule.ErrorMessage);
                    }
                }

                object oldValue = null;
                _configValues.TryGetValue(key, out oldValue);

                _configValues[key] = value;

                // Raise value changed event
                OnValueChanged(key, oldValue, value);

                return new ConfigurationBuilder(this, key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public T GetByPath<T>(string path, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                return defaultValue;
            }

            try
            {
                _lock.EnterReadLock();

                string[] parts = path.Split('.');
                if (parts.Length == 1)
                {
                    // Simple key
                    return Get(path, defaultValue);
                }

                // Navigate through the path
                ConcurrentDictionary<string, object> current = _configValues;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string part = parts[i];
                    if (current.TryGetValue(part, out var value) && value is ConcurrentDictionary<string, object> dict)
                    {
                        current = dict;
                    }
                    else
                    {
                        // Path not found
                        return defaultValue;
                    }
                }

                // Get the final value
                string lastPart = parts[parts.Length - 1];
                if (current.TryGetValue(lastPart, out var finalValue))
                {
                    if (finalValue is T typedValue)
                    {
                        return typedValue;
                    }

                    // Try to convert
                    try
                    {
                        return ConvertValue<T>(finalValue);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                return defaultValue;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Imports configuration from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Result of the operation</returns>
        public OperationResult ImportFromJson(string json)
        {
            // This would need a JSON library like Newtonsoft.Json or System.Text.Json
            // For demo purposes, we'll just return a placeholder
            return OperationResult.Failed("JSON import not implemented");
        }

        /// <summary>
        /// Exports configuration to JSON
        /// </summary>
        /// <returns>JSON string</returns>
        public string ExportToJson()
        {
            // This would need a JSON library like Newtonsoft.Json or System.Text.Json
            // For demo purposes, we'll just return a placeholder
            return "{}";
        }

        private void OnValueChanged(string key, object oldValue, object newValue)
        {
            ValueChanged?.Invoke(this, new ConfigValueChangedEventArgs(key, oldValue, newValue));
        }

        private T ConvertValue<T>(object value)
        {
            if (value == null)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            // Handle common type conversions
            Type targetType = typeof(T);

            if (targetType == typeof(bool) && value is string boolStr)
            {
                return (T)(object)bool.Parse(boolStr);
            }

            if (targetType == typeof(DateTime) && value is string dateStr)
            {
                return (T)(object)DateTime.Parse(dateStr);
            }

            // Use Convert.ChangeType for other conversions
            return (T)Convert.ChangeType(value, targetType);
        }

        private object ParseValue(string type, string value)
        {
            switch (type.ToLower())
            {
                case "int":
                    return int.TryParse(value, out var i) ? i : 0;
                case "bool":
                    return bool.TryParse(value, out var b) && b;
                case "float":
                    return float.TryParse(value, out var f) ? f : 0f;
                case "double":
                    return double.TryParse(value, out var d) ? d : 0.0;
                case "string":
                    return value.Trim('"');
                case "datetime":
                    return DateTime.TryParse(value.Trim('"'), out var dt) ? dt : DateTime.MinValue;
                default:
                    return value;
            }
        }

        private string GetTypeString(object value) => value switch
        {
            int => "int",
            bool => "bool",
            float => "float",
            double => "double",
            string => "string",
            DateTime => "datetime",
            object[] => "array",
            _ => "unknown"
        };
    }
}