using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ConScript
{
    public class ConScript
    {
        private Dictionary<string, object> configValues = new();
        private Dictionary<string, string> comments = new();
        private string encryptionKey = null;

        private static readonly Regex entryPattern = new(@"(\w+)\s+(\w+)\s*=\s*(.*?);", RegexOptions.Compiled);
        private static readonly Regex blockPattern = new(@"(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex typedBlockPattern = new(@"(\w+)\s+(\w+)\s*{", RegexOptions.Compiled);
        private static readonly Regex arrayPattern = new(@"(\w+)\s*\[\]\s*=\s*\[(.*?)\];", RegexOptions.Compiled);
        private static readonly Regex commentPattern = new(@"//(.*)", RegexOptions.Compiled);

        public void Load(string filePath, string password = null)
        {
            if (!File.Exists(filePath)) return;
            configValues.Clear();
            comments.Clear();
            encryptionKey = password;

            string content = File.ReadAllText(filePath);
            if (password != null)
            {
                content = Decrypt(content, password);
            }

            string[] lines = content.Split('\n');
            Stack<Dictionary<string, object>> stack = new();
            Dictionary<string, object> currentBlock = configValues;
            string currentBlockName = null;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (commentPattern.IsMatch(trimmed))
                {
                    comments[trimmed] = trimmed;
                }
                else if (typedBlockPattern.IsMatch(trimmed))
                {
                    var match = typedBlockPattern.Match(trimmed);
                    currentBlockName = match.Groups[2].Value;
                    stack.Push(currentBlock);
                    currentBlock = new Dictionary<string, object>();
                }
                else if (blockPattern.IsMatch(trimmed))
                {
                    var match = blockPattern.Match(trimmed);
                    currentBlockName = match.Groups[1].Value;
                    stack.Push(currentBlock);
                    currentBlock = new Dictionary<string, object>();
                }
                else if (trimmed == "};")
                {
                    if (stack.Count > 0)
                    {
                        var parent = stack.Pop();
                        if (!string.IsNullOrEmpty(currentBlockName))
                        {
                            parent[currentBlockName] = currentBlock;
                        }
                        currentBlock = parent;
                    }
                }
                else if (arrayPattern.IsMatch(trimmed))
                {
                    var match = arrayPattern.Match(trimmed);
                    string key = match.Groups[1].Value;
                    string[] values = match.Groups[2].Value.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                    currentBlock[key] = values;
                }
                else
                {
                    var match = entryPattern.Match(trimmed);
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
            using StringWriter writer = new();
            foreach (var entry in configValues)
            {
                WriteEntry(writer, entry.Key, entry.Value, 0);
            }

            string content = writer.ToString();

            if (encryptionKey != null)
            {
                content = Encrypt(content, encryptionKey);
            }

            File.WriteAllText(filePath, content);
        }

        private void WriteEntry(StringWriter writer, string key, object value, int indent)
        {
            string indentSpace = new string(' ', indent * 4);
            if (comments.ContainsKey(key))
            {
                writer.WriteLine(indentSpace + "// " + comments[key]);
            }
            if (value is Dictionary<string, object> block)
            {
                writer.WriteLine(indentSpace + key + " {");
                foreach (var subEntry in block)
                {
                    WriteEntry(writer, subEntry.Key, subEntry.Value, indent + 1);
                }
                writer.WriteLine(indentSpace + "};");
            }
            else if (value is IEnumerable enumerable && value is not string)
            {
                var items = string.Join(", ", enumerable.Cast<object>());
                writer.WriteLine($"{indentSpace}{key}[] = [{items}];");
            }
            else
            {
                string type = GetTypeString(value);
                string? val = value is string ? "\"" + value + "\"" : value.ToString();
                writer.WriteLine(indentSpace + $"{type} {key} = {val};");
            }
        }

        public void Set<T>(string key, T value, string comment = null)
        {
            configValues[key] = value;
            if (comment != null)
                comments[key] = comment;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (configValues.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        private string Encrypt(string plainText, string password)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(password.PadRight(32));
            aes.IV = new byte[16];
            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using StreamWriter sw = new(cs);
            sw.Write(plainText);
            return Convert.ToBase64String(ms.ToArray());
        }

        private string Decrypt(string encryptedText, string password)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(password.PadRight(32));
                aes.IV = new byte[16];
                using MemoryStream ms = new(Convert.FromBase64String(encryptedText));
                using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader sr = new(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                return "";
            }
        }

        private object ParseValue(string type, string value)
        {
            return type.ToLower() switch
            {
                "int" => int.TryParse(value, out var i) ? i : 0,
                "bool" => bool.TryParse(value, out var b) && b,
                "float" => float.TryParse(value, out var f) ? f : 0f,
                "double" => double.TryParse(value, out var d) ? d : 0.0,
                "string" => value.Trim('"'),
                _ => value
            };
        }

        private string GetTypeString(object value) => value switch
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