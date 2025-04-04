# ConScript

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)

ConScript is a robust, thread-safe library for processing configuration files in the `.cosc` format. It provides a comprehensive solution for storing, loading, and manipulating configuration values with support for various data types, nested structures, and powerful features like encryption and validation.

## 📋 Features

- **Type-Safe Configuration**: Support for various data types including `int`, `float`, `double`, `bool`, `string`, `DateTime`, and arrays
- **Nested Structures**: Easily organize your configurations with hierarchical blocks
- **Thread Safety**: Built-in thread synchronization for safe concurrent access
- **Encryption**: AES-256 encryption with PBKDF2 key derivation for secure configuration storage
- **Validation**: Define validation rules for your configuration values
- **Path-Based Access**: Access nested values using dot notation (e.g., `player.weapons.mainWeapon`)
- **Event System**: Get notified when configuration values change
- **Fluent API**: Elegant, chainable configuration setup
- **Import/Export**: Support for converting to/from other formats (JSON)
- **Robust Error Handling**: Detailed operation results for better error diagnosis

## 🚀 Installation

### NuGet (Recommended)

```bash
dotnet add package ConScript
```

### Manual Installation

Add the `ConScript.cs` file to your project and import the namespace:

```csharp
using ConScript;
```

## 📖 Usage

### Basic Usage

```csharp
// Create a new configuration
var config = new ConScript();

// Load from file
OperationResult result = config.Load("config.cosc");
if (!result.Success)
{
    Console.WriteLine($"Error: {result.Message}");
}

// Access values
int health = config.GetInt("maxHealth");
string name = config.GetString("playerName");
bool isActive = config.GetBool("isActive");
DateTime lastLogin = config.GetDateTime("lastLogin");

// Access nested values
int attackPower = config.GetByPath<int>("player.stats.attackPower");

// Set values with fluent API
config.Set("maxHealth", 100)
      .WithComment("Maximum player health")
      .WithValidation<int>(v => v > 0, "Health must be positive");

// Save to file
result = config.Save("config.cosc");
```

### Advanced Features

#### Encryption

```csharp
// Load encrypted file
config.Load("secure-config.cosc", "mySecretPassword");

// Save with encryption
config.Save("secure-config.cosc");  // Will use the password provided during load
```

#### Value Changed Events

```csharp
// Subscribe to changes
config.ValueChanged += (sender, e) => {
    Console.WriteLine($"Value changed: {e.Key} from {e.OldValue} to {e.NewValue}");
};
```

#### Validation

```csharp
// Set with validation
config.Set("playerAge", 25)
      .WithValidation<int>(age => age >= 18, "Player must be an adult");
```

#### Lists and Arrays

```csharp
// Get list of values
List<string> inventory = config.GetList<string>("inventory");

// Set list of values
config.Set("highScores", new[] { 1000, 800, 650, 500 });
```

## 📝 File Format Example

```cosc
// Player configuration
// Last updated: 2025-03-01
int maxHealth = 100;
string playerName = "PlayerOne";
bool isActive = true;
datetime lastLogin = "2025-04-01T10:30:00";

// Score history
int[] highScores = [1000, 850, 700, 650];

// Nested player details
player {
    string name = "Hero";
    float health = 100.0;
    stats {
        int strength = 15;
        int agility = 12;
        int intelligence = 10;
    };
    equipment {
        string weapon = "Excalibur";
        string armor = "Dragon Plate";
    };
};
```

## 🔄 Conversion

### JSON Import/Export

```csharp
// Export to JSON
string json = config.ExportToJson();

// Import from JSON
config.ImportFromJson(json);
```

## 🧪 Testing

Run the unit tests with:

```bash
dotnet test
```

## 📊 Performance Considerations

- **Thread Safety**: ConScript is designed for concurrent access but heavy parallel writes might affect performance
- **Encryption**: Enabling encryption adds processing overhead, use it only when security is required
- **File Size**: Very large configuration files might impact load/save times

## 📘 API Reference

### Main Classes

- `ConScript`: The main configuration class
- `OperationResult`: Contains the result of operations
- `ConfigValueChangedEventArgs`: Event arguments for value changes

### Interfaces

- `IConfigurationProvider`: Core configuration capabilities
- `IConfigurationBuilder`: Fluent API for configuration
- `IEncryptionService`: Encryption service interface

## ⚖️ License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
