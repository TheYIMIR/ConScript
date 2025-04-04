# ConScript

<div align="center">
  
  ![ConScript Logo](https://via.placeholder.com/200x80?text=ConScript)
  
  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
  [![NuGet Version](https://img.shields.io/badge/NuGet-v1.0.0-blue?style=flat-square&logo=nuget)](https://www.nuget.org/)
  
  *A modern, thread-safe configuration management library for .NET applications*
  
</div>

## 📋 Overview

ConScript is a powerful configuration management library designed for .NET developers who need a robust, secure, and flexible way to handle application settings. With its intuitive API and comprehensive feature set, ConScript simplifies configuration management while providing advanced capabilities like encryption, validation, and hierarchical organization.

## ✨ Features

<table>
  <tr>
    <td><b>🔒 Secure</b></td>
    <td>AES-256 encryption for sensitive configuration with full thread safety</td>
  </tr>
  <tr>
    <td><b>🧩 Flexible</b></td>
    <td>Hierarchical configuration with nested blocks and dot notation access</td>
  </tr>
  <tr>
    <td><b>✅ Type-safe</b></td>
    <td>Strongly typed access with automatic conversion between types</td>
  </tr>
  <tr>
    <td><b>🚦 Validated</b></td>
    <td>Custom validation rules with descriptive error messages</td>
  </tr>
  <tr>
    <td><b>🔄 Reactive</b></td>
    <td>Event-based notification system for tracking configuration changes</td>
  </tr>
  <tr>
    <td><b>⚡ Performant</b></td>
    <td>Optimized for speed with compiled regex patterns and concurrent collections</td>
  </tr>
  <tr>
    <td><b>📝 Documented</b></td>
    <td>Comments support in configuration files with preservation during save</td>
  </tr>
  <tr>
    <td><b>🧪 Extensible</b></td>
    <td>Pluggable architecture with interfaces for custom implementations</td>
  </tr>
</table>

## 🚀 Getting Started

### Installation

Install ConScript via NuGet Package Manager:

```bash
dotnet add package ConScript
```

Or via the Package Manager Console:

```powershell
Install-Package ConScript
```

### Quick Start

```csharp
// Create a new configuration instance
var config = new ConScript();

// Load configuration from file
var result = config.Load("config.cscript");
if (!result.Success)
{
    Console.WriteLine($"Error: {result.Message}");
    return;
}

// Access configuration values with type safety
int maxHealth = config.GetInt("maxHealth");
string playerName = config.GetString("player.name");
float moveSpeed = config.GetFloat("player.moveSpeed");

// Modify configuration with fluent API
config.Set("maxHealth", 200)
      .WithComment("Maximum player health")
      .WithValidation<int>(v => v > 0, "Health must be positive");

// Save configuration
config.Save("config.cscript");
```

## 📖 Usage Examples

### Working with Hierarchical Configuration

ConScript provides intuitive access to nested configuration:

```csharp
// Access nested values with dot notation
int strength = config.GetByPath<int>("player.stats.strength");
float speed = config.GetByPath<float>("player.stats.speed");

// Array access
List<string> abilities = config.GetList<string>("player.abilities");
```

### Encrypted Configuration

Protect sensitive configuration data with built-in encryption:

```csharp
// Load encrypted configuration
var config = new ConScript();
var result = config.Load("secure-config.cscript", "your-password");

// Make changes
config.Set("database.connectionString", "Server=myserver;Database=mydb;User Id=username;Password=password;");

// Save with encryption (uses the password from loading)
config.Save("secure-config.cscript");
```

### Configuration Change Events

Track changes to configuration values:

```csharp
// Subscribe to configuration changes
config.ValueChanged += (sender, args) => {
    Console.WriteLine($"[CONFIG] Value '{args.Key}' changed:");
    Console.WriteLine($"  From: {args.OldValue}");
    Console.WriteLine($"  To:   {args.NewValue}");
    
    // Trigger application logic based on changes
    if (args.Key == "logging.level") {
        UpdateLoggingLevel(args.NewValue);
    }
};
```

## 📜 Configuration File Format

ConScript uses a clean, readable format inspired by modern configuration languages:

```
// Server configuration
server {
    // Network settings with validation
    network {
        int port = 8080;
        string ip = "127.0.0.1";
        bool enableUpnp = true;
    };
    
    // Performance settings
    performance {
        int maxPlayers = 64;
        float tickRate = 64.0;
        int[] workerThreads = [2, 4, 8];
    };
};

// Player settings
player {
    string name = "DefaultPlayer";
    float moveSpeed = 5.0;
    
    // Player statistics block
    stats {
        int health = 100;
        int armor = 50;
        string[] abilities = ["jump", "run", "swim"];
    };
};
```

## 🛠️ Advanced Usage

### Custom Encryption

Implement your own encryption service by implementing the `IEncryptionService` interface:

```csharp
public class CustomEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText, string password)
    {
        // Your custom encryption logic
    }

    public (OperationResult Result, string DecryptedText) Decrypt(string encryptedText, string password)
    {
        // Your custom decryption logic
    }
}

// Use your custom encryption service
var config = new ConScript(new CustomEncryptionService());
```

### Validation Rules

Ensure configuration values meet specific requirements:

```csharp
// Set server port with validation
config.Set("server.network.port", 8080)
      .WithValidation<int>(port => port >= 1024 && port <= 65535, 
                          "Port must be between 1024 and 65535");

// Set player name with validation
config.Set("player.name", "Player1")
      .WithValidation<string>(name => !string.IsNullOrEmpty(name) && name.Length <= 20,
                             "Player name must be between 1 and 20 characters");
```

## 📊 Performance Considerations

ConScript is designed with performance in mind:

- Uses `ConcurrentDictionary` for thread-safe operations
- Implements reader-writer locks for optimized concurrent access
- Pre-compiles regex patterns for faster parsing
- Employs lazy value conversion to minimize unnecessary processing

## 📋 API Reference

### Core Classes

| Class | Description |
|-------|-------------|
| `ConScript` | Main configuration class that implements `IConfigurationProvider` |
| `ConfigurationBuilder` | Fluent API for configuration operations |
| `OperationResult` | Result pattern for operations that may succeed or fail |

### Key Interfaces

| Interface | Description |
|-----------|-------------|
| `IConfigurationProvider` | Core configuration operations |
| `IConfigurationBuilder` | Fluent API for building configurations |
| `IEncryptionService` | Interface for custom encryption implementations |

## 📄 License

ConScript is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 TheYIMIR

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

## 🤝 Contributing

Contributions are welcome! Here's how you can contribute:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please make sure to update tests as appropriate and adhere to the existing coding style.

## ✨ Acknowledgements

- Developed by TheYIMIR
- Built with .NET 8.0
- Inspired by modern configuration systems

---

<div align="center">
  
  Made with ❤️ by [TheYIMIR](https://github.com/TheYIMIR)
  
  Copyright © 2025
  
</div>
