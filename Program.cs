using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);
//builder.Logging.AddConsole(consoleLogOptions =>
//{
//    // Configure all logs to go to stderr
//    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
//});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

public class Result
{
    public int Token;
    public string Name;
}

[McpServerToolType]
public static class DnlibTools
{
    public static ModuleDefMD? Module = null;

    [McpServerTool, Description("Load a .NET assembly into memory")]
    public static void LoadAssembly(
        [Description("Path to the .NET assembly")]
        string AssemblyPath)
    {
        Module = ModuleDefMD.Load(AssemblyPath);
    }

    [McpServerTool, Description("List all types in a .NET assembly")]
    public static String[] ListTypes()
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.Select(t => new
        {
            Name = t.Name.ToString(),
            FullName = t.FullName.ToString(),
            NameSpace = t.Namespace.ToString(),
            Token = t.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { "No types found." };
    }
    
     // Find Types with name match regex
    [McpServerTool, Description("List all types in a .NET assembly matching a regex")]
    public static string[] ListTypesRegex(
        [Description("Regex pattern to match type names")]
        string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        var types = Module.Types.Where(t => regex.IsMatch(t.FullName)).Select(t => new
        {
            Name = t.Name.ToString(),
            FullName = t.FullName.ToString(),
            NameSpace = t.Namespace.ToString(),
            Token = t.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { $"No types matching '{pattern}' found." };
    }
    
    // List methods of a type name
    [McpServerTool, Description("List all methods in a .NET assembly")]
    public static string[] ListMethods(
        [Description("Full Name of the type")]
        string typeName)
    {
        var type = Module.Types.FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
            return new[] { $"Type '{typeName}' not found." };

        var methods = type.Methods.Select(m => new
        {
            Name = m.Name.ToString(),
            FullName = m.FullName.ToString(),
            Token = m.MDToken.ToInt32(),
            Parameters = m.MethodSig.Params.Select(p => p.ToString()).ToArray(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        
        return methods.Length > 0 ? methods : new[] { $"No methods found for '{typeName}'." };
    }
    
    // Find Methods with name match regex
    [McpServerTool, Description("Find methods name matching a regex pattern")]
    public static string[] FindMethodsWithRegex(
        [Description("Regex pattern to match method names")]
        string pattern)
    {
        var matchingMethods = Module.Types
            .SelectMany(t => t.Methods)
            .Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, pattern))
            .Select(t => new
            {
                Name = t.Name.ToString(),
                FullName = t.FullName.ToString(),
                Token = t.MDToken.ToInt32(),
                Parameters = t.MethodSig.Params.Select(p => p.ToString()).ToArray(),
            }).Select(t => JsonSerializer.Serialize(t)).ToArray();
            
        return matchingMethods.Length > 0 ? matchingMethods : new[] { $"No methods matching '{pattern}' found." };
    }
    
    [McpServerTool, Description("List all fields in a .NET assembly")]
    public static string[] ListFields()
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Fields).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            Token = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { "No types found." };
    }
    
    
    [McpServerTool, Description("List all properties in a .NET assembly")]
    public static string[] ListProperties()
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Properties).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            Token = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { "No properties found." };
    }
    [McpServerTool, Description("List all events in a .NET assembly")]
    public static string[] ListEvents()
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Events).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            Token = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { "No properties found." };
    }
    
    [McpServerTool, Description("List all resources in a .NET assembly")]
    public static string[] ListResources()
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var types = Module.Resources.Select(f => new
        {
            Name = f.Name.ToString(),
            ResourceType = f.ResourceType.ToString(),
            Token = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? types : new[] { "No data found." };
    }

    [McpServerTool, Description("Get detailed information about a specific type")]
    public static string GetTypeInfo(string typeName)
    {
        var type = Module.Types.FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
            return $"Type '{typeName}' not found.";

        // Get detailed information about the type
        return $"Name: {type.Name}\n" +
               $"Namespace: {type.Namespace}\n" +
               $"Base Type: {type.BaseType}\n" +
               $"Attributes: {type.Attributes}\n" +
               $"Is Public: {type.IsPublic}\n" +
               $"Is Sealed: {type.IsSealed}\n" +
               $"Is Abstract: {type.IsAbstract}\n" +
               $"Is Interface: {type.IsInterface}\n" +
               $"Is ValueType: {type.IsValueType}";
    }
    
    [McpServerTool, Description("Find string literals in the assembly")]
    public static string[] FindStringLiterals()
    {
        var strings = new HashSet<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null)
                    continue;
                    
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is string str)
                    {
                        var data = new
                        {
                            MethodName = method.Name.ToString(),
                            FullName = method.FullName.ToString(),
                            Token = method.MDToken.ToInt32(),
                            StringLiteral = str,
                            InstrOffset = instr.Offset,
                        };
                        strings.Add(JsonSerializer.Serialize(data));
                    }
                }
            }
        }
        
        return strings.ToArray();
    }

    [McpServerTool, Description("Search for types matching a pattern")]
    public static string[] SearchTypes(string pattern)
    {
        var matchingTypes = Module.Types
            .Where(t => t.FullName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .Select(f => new
            {
                Name = f.Name.ToString(),
                FullName = f.FullName.ToString(),
                Token = f.MDToken.ToInt32(),
                Namespace = f.Namespace.ToString(),
            }).Select(t => JsonSerializer.Serialize(t)).ToArray();
            
        return matchingTypes.Length > 0 ? matchingTypes : new[] { $"No types matching '{pattern}' found." };
    }

    [McpServerTool, Description("Examine constructor initialization for a type")]
    public static string[] ExamineConstructors(string typeName)
    {
        var type = Module.Types.FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
            return new[] { $"Type '{typeName}' not found." };
            
        var ctors = new List<string>();
        foreach (var ctor in type.Methods.Where(m => m.IsConstructor))
        {
            ctors.Add($"{ctor.FullName}, IsStatic: {ctor.IsStatic}, Parameters: {ctor.Parameters.Count}");
            
            if (ctor.Body != null)
            {
                foreach (var instr in ctor.Body.Instructions)
                {
                    ctors.Add($"  {instr.Offset:X4}: {instr}");
                }
            }
        }
        
        return ctors.Count > 0 ? ctors.ToArray() : new[] { $"No constructors found for '{typeName}'." };
    }

    [McpServerTool, Description("List external dependencies used by a specific type")]
    public static string[] ListTypeDependencies(string typeName)
    {
        var type = Module.Types.FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
            return new[] { $"Type '{typeName}' not found." };
            
        var dependencies = new HashSet<string>();
        
        // Check base type
        if (type.BaseType != null)
            dependencies.Add($"Inherits: {type.BaseType.FullName}");
            
        // Check interfaces
        foreach (var intf in type.Interfaces)
        {
            dependencies.Add($"Implements: {intf.Interface.FullName}");
        }
        
        // Check methods
        foreach (var method in type.Methods)
        {
            if (method.Body == null) continue;
            
            foreach (var instr in method.Body.Instructions)
            {
                if (instr.Operand is ITypeDefOrRef typeRef)
                {
                    dependencies.Add($"References: {typeRef.FullName}");
                }
                else if (instr.Operand is IMethod methodRef)
                {
                    dependencies.Add($"Calls: {methodRef.FullName}");
                }
                else if (instr.Operand is IField fieldRef)
                {
                    dependencies.Add($"Uses Field: {fieldRef.FullName}");
                }
            }
        }
        
        return dependencies.ToArray();
    }

    [McpServerTool, Description("Find all usages of a specified method")]
    public static string[] FindMethodUsages(string methodName)
    {
        var usages = new List<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is IMethod calledMethod && 
                        calledMethod.FullName.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                    {
                        usages.Add($"{method.FullName} calls {calledMethod.FullName} at IL_{instr.Offset:X4}");
                    }
                }
            }
        }
        
        return usages.Count > 0 ? usages.ToArray() : new[] { $"No usages of '{methodName}' found." };
    }

    [McpServerTool, Description("Find possible reflection usage in the assembly")]
    public static string[] FindReflectionUsage()
    {
        var results = new List<string>();
        string[] reflectionPatterns = new[] {
            "System.Reflection",
            "GetType",
            "InvokeMember",
            "Invoke",
            "Assembly.Load",
            "GetMethod",
            "GetField",
            "GetProperty"
        };
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is IMethod calledMethod)
                    {
                        foreach (var pattern in reflectionPatterns)
                        {
                            if (calledMethod.FullName.Contains(pattern))
                            {
                                results.Add($"{method.FullName}: {calledMethod.FullName} at IL_{instr.Offset:X4}");
                                break;
                            }
                        }
                    }
                    else if (instr.Operand is string str)
                    {
                        foreach (var pattern in reflectionPatterns)
                        {
                            if (str.Contains(pattern))
                            {
                                results.Add($"{method.FullName}: String literal \"{str}\" at IL_{instr.Offset:X4}");
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { "No reflection usage found." };
    }

    [McpServerTool, Description("Extract method control flow graph")]
    public static string[] ExtractControlFlowGraph(string methodName)
    {
        var results = new List<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.FullName.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                {
                    if (method.Body == null)
                    {
                        results.Add($"Method {method.FullName} has no body");
                        continue;
                    }
                    
                    results.Add($"Control flow graph for {method.FullName}:");
                    
                    var instructions = method.Body.Instructions;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        var instr = instructions[i];
                        results.Add($"IL_{instr.Offset:X4}: {instr}");
                        
                        // Check branch instructions
                        if (instr.OpCode.FlowControl == dnlib.DotNet.Emit.FlowControl.Branch ||
                            instr.OpCode.FlowControl == dnlib.DotNet.Emit.FlowControl.Cond_Branch)
                        {
                            if (instr.Operand is dnlib.DotNet.Emit.Instruction targetInstr)
                            {
                                results.Add($"  -> Jumps to IL_{targetInstr.Offset:X4}");
                            }
                        }
                    }
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No methods matching '{methodName}' found." };
    }
    
    // Dump IL code for a method
    [McpServerTool, Description("Get IL codes for a method")]
    public static string[] GetMethodILBody(string methodName)
    {
        var results = new List<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.FullName.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                {
                    if (method.Body == null)
                    {
                        results.Add($"Method {method.FullName} has no body");
                        continue;
                    }
                    
                    results.Add($"IL code for {method.FullName}:");
                    
                    foreach (var instr in method.Body.Instructions)
                    {
                        results.Add($"IL_{instr.Offset:X4}: {instr}");
                    }
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No methods matching '{methodName}' found." };
    }
}