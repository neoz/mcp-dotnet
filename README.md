# MCPPOC - .NET Reverse Engineering Tools

MCPPOC is a .NET reverse engineering toolset built using the dnlib library. It provides a collection of utilities to analyze .NET assemblies, extract metadata, and inspect code structures. These tools are designed to assist in understanding and debugging .NET applications.

## Features

- **Load Assemblies**: Load .NET assemblies into memory for analysis.
- **List Types**: Enumerate all types in the assembly.
- **Search Types**: Search for types by name or pattern.
- **List Methods**: List all methods in a specific type.
- **Find Method Usages**: Identify where a specific method is called.
- **Inspect IL Code**: Extract and analyze IL instructions for methods.
- **Find String Literals**: Locate hardcoded strings in the assembly.
- **Analyze Exception Handling**: Examine exception handling blocks in methods.
- **Detect Reflection Usage**: Identify potential reflection usage in the code.
- **Detect Dynamic Code Execution**: Find instances of dynamic code generation or execution.
- **Extract Control Flow Graphs**: Visualize the control flow of methods.
- **List Dependencies**: Identify external dependencies for specific types.
- **Find Serialization Usage**: Detect serialization-related code.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/neoz/mcp-dotnet
   cd mcppoc
   ```

2. Build the project using the .NET CLI:
   ```bash
   dotnet build
   ```

3. Run the tool:
   ```bash
   dotnet run
   ```

## Usage

### Loading an Assembly
To load an assembly into memory:
```csharp
DnlibTools.LoadAssembly("path/to/assembly.dll");
```

### Listing Types
To list all types in the loaded assembly:
```csharp
var types = DnlibTools.ListTypes();
```

### Searching for Types
To search for types matching a regex pattern:
```csharp
var matchingTypes = DnlibTools.ListTypesRegex("pattern");
```

### Inspecting Methods
To list all methods in a specific type:
```csharp
var methods = DnlibTools.ListMethods("Namespace.TypeName");
```

### Finding Method Usages
To find all usages of a specific method:
```csharp
var usages = DnlibTools.FindMethodUsages("MethodName");
```

### Detecting Reflection Usage
To detect reflection usage in the assembly:
```csharp
var reflectionUsage = DnlibTools.FindReflectionUsage();
```

### Extracting Control Flow Graphs
To extract the control flow graph of a method:
```csharp
var cfg = DnlibTools.ExtractControlFlowGraph("Namespace.TypeName.MethodName");
```

## MCP Tool Guidelines

When using MCP tools, follow these guidelines to ensure proper usage:

1. **Load the Assembly First**: Always load the target assembly using `DnlibTools.LoadAssembly()` before invoking any other MCP tool. Without a loaded assembly, most tools will return an error like "No assembly loaded."

2. **Understand the Tool's Purpose**: Each MCP tool is designed for a specific purpose. For example:
   - Use `ListTypes()` to get an overview of all types in the assembly.
   - Use `FindMethodUsages()` to trace where a specific method is called.
   - Use `FindReflectionUsage()` to detect dynamic behavior in the code.

3. **Use Regex Tools Carefully**: When using tools like `ListTypesRegex()` or `FindMethodsWithRegex()`, ensure your regex pattern is correct. Incorrect patterns may lead to no results or unexpected matches.

4. **Inspect IL Code with Caution**: Tools like `GetMethodIL()` and `ExtractControlFlowGraph()` provide low-level details about the code. Familiarity with IL (Intermediate Language) is recommended to interpret the results.

5. **Handle Large Assemblies**: For large assemblies, some tools (e.g., `FindStringLiterals()`) may take longer to execute. Be patient and consider narrowing your search scope if possible.

6. **Respect Legal and Ethical Boundaries**: Ensure you have the right to analyze the target assembly. Reverse engineering proprietary software without permission may violate laws or agreements.

7. **Save Results**: Many tools return arrays of strings. Save the results to a file or log them for further analysis:
   ```csharp
   File.WriteAllLines("output.txt", results);
   ```

8. **Debugging and Troubleshooting**: If a tool doesn't return the expected results:
   - Verify the assembly is loaded correctly.
   - Check the input parameters (e.g., type names, method names, regex patterns).
   - Use simpler tools like `ListTypes()` to confirm the assembly's structure.

## Dependencies

- [dnlib](https://github.com/0xd4d/dnlib): A robust library for reading and writing .NET assemblies.
- .NET 9.0 or later.

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to suggest improvements or report bugs.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Disclaimer

This tool is intended for educational and debugging purposes only. Use it responsibly and ensure compliance with applicable laws and regulations.