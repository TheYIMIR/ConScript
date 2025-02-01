# ConScript

ConScript is a simple and flexible library for processing configuration files in the `.cosc` format. It allows you to store, load, and edit configuration values, supporting various data types and nested structures.

## Features

- Store and load configuration data in the `.cosc` format.
- Support for different data types: `int`, `float`, `bool`, `string`, `array`, and custom types.
- Easy handling of nested structures and arrays.
- Ability to load configuration values into lists.
- Flexibility in parsing complex data structures such as blocks and arrays.

## Installation

Add the `ConScript` file to your project and import the namespace:

```csharp
using ConScript;
```

## Usage

### Loading Configuration

Load a configuration file using the `Load` method:

```csharp
ConScript config = new ConScript();
config.Load("path/to/your/config.cosc");
```

### Saving Configuration

Save the configuration using the `Save` method:

```csharp
config.Save("path/to/your/config.cosc");
```

### Getting Values

Use the `Get` method to retrieve configuration values:

```csharp
int someInt = config.GetInt("someIntKey");
float someFloat = config.GetFloat("someFloatKey");
bool someBool = config.GetBool("someBoolKey");
string someString = config.GetString("someStringKey");
List<string> someList = config.GetList<string>("someListKey");
```

or

```csharp
int someInt = config.Get<int>("someIntKey");
float someFloat = config.Get<float>("someFloatKey");
bool someBool = config.Get<bool>("someBoolKey");
string someString = config.Get<string>("someStringKey");
```

### Setting Values

Use the `Set` method to set configuration values:

```csharp
config.Set("someIntKey", 42);
config.Set("someStringKey", "Hello World");
```

## Example `.cosc` File

```cosc
// Simple entries
int maxHealth = 100;
string playerName = "PlayerOne";
bool isActive = true;

// Array
int[] scores = [10, 20, 30];

// Nested structures
player {
    string name = "Player";
    float health = 100;
    float speed = 5.0;
    weapons {
        string mainWeapon = "Sword";
        string secondaryWeapon = "Shield";
    };
};

// Block with type
float playerStats {
    jumpHeight = 2.5;
    stamina = 100;
};
```

# License

This project is licensed under the MIT License.
