using ConScript.Core;
using ConScript.Interfaces;
using ConScript.Models;
using ConScript.Services;
using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace ConScript
{
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