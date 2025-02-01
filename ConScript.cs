using System.Text.RegularExpressions;

namespace ConScript
{
    public class ConScript
    {
        private Dictionary<string, object> configValues = new();
        private static readonly Regex entryPattern = new(@"(\w+)\s+(\w+)\s*=\s*(.*?);", RegexOptions.Compiled);
        private static readonly Regex blockPattern = new(@"(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex typedBlockPattern = new(@"(\w+)\s+(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex arrayPattern = new(@"(\w+)\s*\[\]\s*=\s*\[(.*?)\];", RegexOptions.Compiled);

        public void Load(string filePath)
        {
            if (!File.Exists(filePath)) return;
            configValues.Clear();
            string[] lines = File.ReadAllLines(filePath);
            Stack<Dictionary<string, object>> stack = new();
            Dictionary<string, object> currentBlock = configValues;
            string currentBlockName = null;

            foreach (var line in lines)
            {
                if (typedBlockPattern.IsMatch(line))
                {
                    var match = typedBlockPattern.Match(line);
                    currentBlockName = match.Groups[2].Value;
                    stack.Push(currentBlock);
                    currentBlock = new Dictionary<string, object>();
                }
                else if (blockPattern.IsMatch(line))
                {
                    var match = blockPattern.Match(line);
                    currentBlockName = match.Groups[1].Value;
                    stack.Push(currentBlock);
                    currentBlock = new Dictionary<string, object>();
                }
                else if (line.Trim() == "};")
                {
                    var parent = stack.Pop();
                    parent[currentBlockName] = currentBlock;
                    currentBlock = parent;
                }
                else if (arrayPattern.IsMatch(line))
                {
                    var match = arrayPattern.Match(line);
                    string key = match.Groups[1].Value;
                    string[] values = match.Groups[2].Value.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                    currentBlock[key] = values;
                }
                else
                {
                    var match = entryPattern.Match(line);
                    if (match.Success)
                    {
                        string type = match.Groups[1].Value;
                        string key = match.Groups[2].Value;
                        string value = match.Groups[3].Value;
                        currentBlock[key] = ParseValue(type, value);
                    }
                }
            }
        }

        public void Save(string filePath)
        {
            using StreamWriter writer = new(filePath);
            foreach (var entry in configValues)
            {
                WriteEntry(writer, entry.Key, entry.Value, 0);
            }
        }

        private void WriteEntry(StreamWriter writer, string key, object value, int indent)
        {
            string indentSpace = new string(' ', indent * 4);
            if (value is Dictionary<string, object> block)
            {
                writer.WriteLine(indentSpace + key + " {");
                foreach (var subEntry in block)
                {
                    WriteEntry(writer, subEntry.Key, subEntry.Value, indent + 1);
                }
                writer.WriteLine(indentSpace + "};");
            }
            else if (value is object[] array)
            {
                writer.WriteLine(indentSpace + key + "[] = [" + string.Join(", ", array) + "]; ");
            }
            else
            {
                string type = GetTypeString(value);
                string? val = value is string ? "\"" + value + "\"" : value.ToString();
                writer.WriteLine(indentSpace + $"{type} {key} = {val};");
            }
        }

        public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);
        public float GetFloat(string key, float defaultValue = 0f) => Get(key, defaultValue);
        public double GetDouble(string key, double defaultValue = 0.0) => Get(key, defaultValue);
        public string GetString(string key, string defaultValue = "") => Get(key, defaultValue);
        public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);
        public List<T> GetList<T>(string key) => Get<object[]>(key)?.Select(x => (T)Convert.ChangeType(x, typeof(T))).ToList() ?? new List<T>();

        public T Get<T>(string key, T defaultValue = default)
        {
            if (configValues.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            configValues[key] = value;
        }

        private object ParseValue(string type, string value)
        {
            return type.ToLower() switch
            {
                "int" => int.Parse(value),
                "bool" => bool.Parse(value),
                "float" => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "double" => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                "string" => value.Trim('"'),
                _ => value
            };
        }

        private string GetTypeString(object value)
        {
            return value switch
            {
                int => "int",
                bool => "bool",
                float => "float",
                double => "double",
                string => "string",
                object[] => "array",
                _ => "unknown"
            };
        }
    }

}