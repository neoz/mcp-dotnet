# MCPPOC - .NET Reverse Engineering via MCP

MCPPOC is a .NET reverse engineering toolkit implemented as a Model Context Protocol (MCP) server. Built using the dnlib library, it provides an API-driven approach to analyze .NET assemblies, extract metadata, and inspect code structures through the MCP communication protocol.

## Features

- **Assembly Analysis**: Load and examine .NET assemblies via MCP API calls
- **Type Discovery**: Search and enumerate all types using regex patterns
- **Method Inspection**: List methods, analyze IL code, and extract control flow graphs
- **Code Relationships**: Find method usages, dependencies, and references
- **String Analysis**: Locate literal strings embedded in assemblies
- **Advanced Analysis**: Detect reflection usage, dynamic code execution, and serialization
- **Code Decompilation**: Convert IL code back to C# for easier analysis
- **Code Patching**: Modify method instructions for testing and debugging

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/neoz/mcp-dotnet
   cd mcp-dotnet
   ```

2. Build the project using the .NET CLI:
   ```bash
   dotnet build
   ```

3. Run the MCP server:
   ```bash
   dotnet run
   ```

## Using MCPPOC as an MCP Server

MCPPOC implements the Model Context Protocol (MCP) to expose its .NET reverse engineering capabilities as a service. This allows any MCP-compatible client to interact with the toolkit and analyze .NET assemblies.

### Connecting to MCPPOC

When running, MCPPOC provides an MCP server that processes commands through standard input/output. You can:

1. Connect directly via stdin/stdout when running the executable
2. Connect through an MCP-compatible client tool
3. Integrate into editors and IDEs that support MCP

### Available MCP Tools

The `DnlibTools` class exposes all functionality as MCP server tools with the `[McpServerTool]` attribute. Each tool is self-documented with descriptive parameter information.

#### Basic Operations

```json
// Load an assembly for analysis
{
  "jsonrpc": "2.0",
  "method": "mcp/execute",
  "params": {
    "tool": "LoadAssembly",
    "args": {
      "AssemblyPath": "path/to/assembly.dll"
    }
  }
}

// List types matching a pattern
{
  "jsonrpc": "2.0",
  "method": "mcp/execute",
  "params": {
    "tool": "ListTypesRegex",
    "args": {
      "pattern": "System\\..*",
      "offset": 0,
      "pageSize": 100
    }
  }
}
```

#### Advanced Analysis

```json
// Decompile a method using its RID
{
  "jsonrpc": "2.0",
  "method": "mcp/execute",
  "params": {
    "tool": "Decompile_Method_By_RID",
    "args": {
      "rid": 42
    }
  }
}

// Extract a method's control flow graph
{
  "jsonrpc": "2.0",
  "method": "mcp/execute",
  "params": {
    "tool": "ExtractControlFlowGraph",
    "args": {
      "methodName": "Namespace.TypeName.MethodName"
    }
  }
}
```

## MCP Server Architecture

MCPPOC implements the Model Context Protocol (MCP) server architecture, which offers several advantages:

1. **Language-Agnostic**: Any language or tool that can speak MCP can use MCPPOC's functionality
2. **IDE Integration**: Easily integrates with VS Code and other MCP-compatible editors
3. **Self-Documented**: All tools expose descriptions and parameter information
4. **Structured Data**: Returns JSON-serialized results for easy parsing and analysis

### MCP Tool Guidelines

When using MCPPOC via MCP, follow these guidelines for optimal results:

1. **Sequential Workflow**: Always start by calling the `LoadAssembly` tool before using any analysis tools. All operations depend on having an assembly loaded first.

2. **Tool Selection Strategy**: 
   - For discovery: Use `ListTypesRegex` and `SearchTypes` to find structures of interest
   - For analysis: Use specialized tools like `GetMethodILBodyByRID` or `ListTypeDependencies`
   - For modification: Use `UpdateMethodInstructions` to patch code

3. **Pagination Support**: Many tools support pagination with `offset` and `pageSize` parameters:
   ```json
   {
     "tool": "FindStringLiterals",
     "args": {
       "offset": 100,
       "pageSize": 50
     }
   }
   ```

4. **Error Handling**: All tools return meaningful error messages when operations fail:
   - "No assembly loaded" indicates you need to call `LoadAssembly` first
   - Type/method not found errors indicate you should verify names and use search tools

5. **Working with Large Assemblies**: 
   - Use specific search tools rather than retrieving everything
   - Increase page sizes for bulk operations
   - Consider extracting only the needed components

6. **Security and Permissions**: Respect intellectual property and license restrictions when analyzing assemblies. The tool provides capabilities for legitimate reverse engineering scenarios only.

## Example MCP Session

Here's an example of a complete reverse engineering session using the MCP protocol:

```json
// 1. Load an assembly
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"LoadAssembly","args":{"AssemblyPath":"SampleApp.dll"}}}
// Response: {"jsonrpc":"2.0","id":null,"result":"Assembly 'SampleApp.dll' loaded successfully."}

// 2. Get metadata about the assembly
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"GetMetadata","args":{}}}
// Response: {"jsonrpc":"2.0","id":null,"result":"{\"AssemblyName\":\"SampleApp\",\"EntryPoint\":\"SampleApp.Program::Main\",\"MixedMode\":false}"}

// 3. Find interesting string literals
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"FindStringLiterals","args":{"offset":0,"pageSize":10}}}
// Response: [Array of string references]

// 4. Find methods that use a specific string
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"FindStringReferences","args":{"str":"password"}}}
// Response: [Array of method references]

// 5. Decompile an interesting method
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"Decompile_Method_By_RID","args":{"rid":42}}}
// Response: C# decompiled code
```

## Advanced Features

### Control Flow Analysis

MCPPOC can extract and visualize method control flows, which is valuable for understanding complex methods:

```json
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"ExtractControlFlowGraph","args":{"methodName":"Namespace.TypeName.MethodName"}}}
```

### Code Patching

You can modify methods at the IL level for testing or debugging:

```json
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"UpdateMethodInstructions","args":{"rid":42,"offset":10,"newInstruction":"ldstr \"Patched!\"\ncall void [System.Console]System.Console::WriteLine(string)"}}}
```

### Assembly Saving

After analyzing or modifying an assembly, save the changes:

```json
{"jsonrpc":"2.0","method":"mcp/execute","params":{"tool":"SaveAssembly","args":{"path":"Modified.dll","mixedMode":false}}}
```

## Integration Guide

### Setting Up with VS Code

VS Code can connect to MCPPOC as an MCP server to provide advanced .NET analysis capabilities directly within your editor:

1. Install the VS Code MCP Client extension:
   ```
   code --install-extension ms-toolsai.vscode-mcp-client
   ```

2. Configure the extension in your `settings.json`:
   ```json
   "mcp.servers": [
     {
       "name": "MCPPOC",
       "command": "dotnet",
       "args": ["path/to/MCPPOC.dll"],
       "capabilities": ["dnlib", "decompiler"]
     }
   ]
   ```

3. Start the server from the Command Palette:
   ```
   > MCP: Connect to Server > MCPPOC
   ```

4. Access MCPPOC tools from the VS Code MCP Client panel.

### Using with Claude AI

Claude can interact with MCPPOC to analyze .NET assemblies when properly configured:

1. Set up a Claude AI environment with MCP capabilities enabled.

2. Configure Claude to use MCPPOC as its MCP server:
   ```json
   {
     "mcpServers": [
       {
         "id": "dotnet-analyzer",
         "name": "MCPPOC",
         "command": "dotnet run --project /path/to/mcp-dotnet/MCPPOC.csproj",
         "documentationUrl": "https://github.com/neoz/mcp-dotnet"
       }
     ]
   }
   ```

3. In your conversations with Claude, you can instruct it to use MCPPOC tools:
   ```
   Claude, please use the MCPPOC MCP server to analyze the MyApp.dll assembly and find all string literals.
   ```

### Command-Line Integration

For command-line usage, you can create scripts that communicate with MCPPOC via standard I/O:

#### PowerShell Example

```powershell
# Start MCPPOC server
$mcpServer = Start-Process -FilePath "dotnet" -ArgumentList "run --project C:\path\to\mcp-dotnet" -RedirectStandardInput stdin.pipe -RedirectStandardOutput stdout.pipe -NoNewWindow -PassThru

# Load an assembly
$request = @{
  jsonrpc = "2.0"
  id = 1
  method = "mcp/execute"
  params = @{
    tool = "LoadAssembly"
    args = @{
      AssemblyPath = "C:\path\to\assembly.dll"
    }
  }
} | ConvertTo-Json -Compress

# Send request to MCPPOC
$request | Out-File -FilePath stdin.pipe -Encoding utf8
$response = Get-Content -Path stdout.pipe

# Process response
Write-Host $response
```

#### Bash Example

```bash
# Start MCPPOC server
mkfifo request_pipe response_pipe
dotnet run --project /path/to/mcp-dotnet < request_pipe > response_pipe &

# Load an assembly
echo '{"jsonrpc":"2.0","id":1,"method":"mcp/execute","params":{"tool":"LoadAssembly","args":{"AssemblyPath":"/path/to/assembly.dll"}}}' > request_pipe

# Read response
cat response_pipe
```

## Dependencies

- [dnlib](https://github.com/0xd4d/dnlib): Library for reading and writing .NET assemblies
- [ICSharpCode.Decompiler](https://github.com/icsharpcode/ILSpy): .NET decompiler for C# code generation
- [ModelContextProtocol](https://github.com/microsoft/modelContextProtocol): MCP server implementation
- .NET 9.0 or later

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to suggest improvements or report bugs.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Disclaimer

This tool is intended for educational and debugging purposes only. Use it responsibly and ensure compliance with applicable laws and regulations.