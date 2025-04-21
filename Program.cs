using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using MCPPOC;

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

[McpServerToolType, Description("MCP Server for Dotnet Reverse Engineering")]
public static class DnlibTools
{
    public static ModuleDefMD? Module = null;
    public static InstructionParser parser = null;

    [McpServerTool, Description("Load a .NET assembly into memory")]
    public static string LoadAssembly(
        [Description("Path to the .NET assembly")]
        string AssemblyPath)
    {
        try
        {
            // Load reference assembly module for resolving types
            var resolver = new AssemblyResolver();
            var moduleContext = new ModuleContext(resolver);
            
            // Create an assembly resolver that can find referenced assemblies
            resolver.DefaultModuleContext = moduleContext;
            resolver.EnableTypeDefCache = true;
            
            // Load the main module
            Module = ModuleDefMD.Load(AssemblyPath, moduleContext);
            resolver.AddToCache(Module);
            
            // Load referenced assemblies
            foreach (var asmRef in Module.GetAssemblyRefs())
            {
                try
                {
                    // Resolve and load the referenced assembly
                    var assembly = resolver.Resolve(asmRef, Module);
                    resolver.AddToCache(assembly);
                }
                catch (Exception ex)
                {
                    return $"Failed to load assembly: {ex.Message}";
                }
            }
            Module = ModuleDefMD.Load(AssemblyPath,context: moduleContext);
            if (Module != null)
            {
                parser = new InstructionParser(Module);
            }
            return $"Assembly '{AssemblyPath}' loaded successfully.";
        }
        catch (Exception ex)
        {
            return $"Failed to load assembly: {ex.Message}";
        }
    }
    
    public static bool IsMixedModeAssembly()
    {
        if (Module == null)
            return false;
        
        // Check if the module is a native or mixed-mode module
        if (Module.IsILOnly)
        {
            // ILOnly indicates pure managed code
            return false;
        }

        // Check if there is a native entry point
        if (Module.NativeEntryPoint != 0)
        {
            return true;
        }

        // Check module kind (optional, for additional context)
        if (Module.Kind == ModuleKind.Windows || Module.Kind == ModuleKind.Dll)
        {
            // Win32 or Native DLLs may contain native code
            // Further checks (e.g., for native methods) could be added here
            return true;
        }

        // If none of the above conditions are met, assume it's not mixed mode
        return false;
    }
    
    // Get metadata of the assembly
    [McpServerTool, Description("Get metadata of the assembly")]
    public static string GetMetadata()
    {
        if (Module == null)
            return "No assembly loaded.";
        
        var metadata = new
        {
            AssemblyName = Module.Name,
            EntryPoint = Module.EntryPoint?.FullName.ToString(),
            MixedMode = IsMixedModeAssembly()
        };
        
        return JsonSerializer.Serialize(metadata);
    }
    
    // Get entry point method
    [McpServerTool, Description("Get entry point method")]
    public static string GetEntryPoint()
    {
        if (Module == null)
            return "No assembly loaded.";
        
        // Get static constructor
        var staticConstructor = Module.Types
            .SelectMany(t => t.Methods)
            .FirstOrDefault(m => m.IsConstructor && m.IsStatic);
        
        var entryPoint = Module.EntryPoint;
        if (entryPoint == null)
            return "No entry point found.";

        var data = new
        {
            EntryPoint = entryPoint.FullName.ToString(),
            EntryPoint_MDToken = entryPoint.MDToken.ToInt32(),
            EntryPoint_RID = entryPoint.Rid,
            StaticConstructor = staticConstructor?.FullName.ToString(),
            StaticConstructor_MDToken = staticConstructor?.MDToken.ToInt32(),
            StaticConstructor_RID = staticConstructor?.Rid,
        };
        
        return JsonSerializer.Serialize(data);
    }

    [McpServerTool, Description("List all types in a .NET assembly")]
    public static String[] ListTypes(
            [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
            [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100)
    
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.Select(t => new
        {
            Name = t.Name.ToString(),
            FullName = t.FullName.ToString(),
            NameSpace = t.Namespace.ToString(),
            MDToken = t.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset,pageSize) : new[] { "No types found." };
    }
    
     // Find Types with name match regex
    [McpServerTool, Description("List all types in a .NET assembly matching a regex")]
    public static string[] ListTypesRegex(
        [Description("Regex pattern to match type names")]
        string pattern,
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        var types = Module.Types.Where(t => regex.IsMatch(t.FullName)).Select(t => new
        {
            Name = t.Name.ToString(),
            FullName = t.FullName.ToString(),
            NameSpace = t.Namespace.ToString(),
            Token = t.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset,pageSize) : new[] { $"No types matching '{pattern}' found." };
    }
    
    // List methods of a type name
    [McpServerTool, Description("List all methods in a .NET assembly")]
    public static string[] ListMethods(
        [Description("Full Name of the type")]
        string typeName,
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var type = Module.Types.FirstOrDefault(t => t.FullName == typeName);
        if (type == null)
            return new[] { $"Type '{typeName}' not found." };

        var methods = type.Methods.Select(m => new
        {
            Name = m.Name.ToString(),
            FullName = m.FullName.ToString(),
            MDToken = m.MDToken.ToInt32(),
            RID = m.Rid,
            Parameters = m.MethodSig.Params.Select(p => p.ToString()).ToArray(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        
        return methods.Length > 0 ? paginate.Paginate(methods,offset, pageSize) : new[] { $"No methods found for '{typeName}'." };
    }
    
    // Find Methods with name match regex
    [McpServerTool, Description("Find methods name matching a regex pattern")]
    public static string[] FindMethodsWithRegex(
        [Description("Regex pattern to match method names")]
        string pattern,
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        var matchingMethods = Module.Types
            .SelectMany(t => t.Methods)
            .Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.Name, pattern))
            .Select(t => new
            {
                Name = t.Name.ToString(),
                FullName = t.FullName.ToString(),
                MDToken = t.MDToken.ToInt32(),
                RID = t.Rid,
                Parameters = t.MethodSig.Params.Select(p => p.ToString()).ToArray(),
            }).Select(t => JsonSerializer.Serialize(t)).ToArray();
            
        return matchingMethods.Length > 0 ? paginate.Paginate(matchingMethods,offset, pageSize) : new[] { $"No methods matching '{pattern}' found." };
    }
    
    [McpServerTool, Description("List all fields in a .NET assembly")]
    public static string[] ListFields(
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Fields).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            MDToken = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset, pageSize) : new[] { "No fields found." };
    }
    
    
    [McpServerTool, Description("List all properties in a .NET assembly")]
    public static string[] ListProperties(
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Properties).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            MDToken = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset, pageSize) : new[] { "No properties found." };
    }
    [McpServerTool, Description("List all events in a .NET assembly")]
    public static string[] ListEvents(
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var types = Module.Types.SelectMany(t => t.Events).Select(f => new
        {
            Name = f.Name.ToString(),
            FullName = f.FullName.ToString(),
            MDToken = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset, pageSize) : new[] { "No properties found." };
    }
    
    [McpServerTool, Description("List all resources in a .NET assembly")]
    public static string[] ListResources(
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var types = Module.Resources.Select(f => new
        {
            Name = f.Name.ToString(),
            ResourceType = f.ResourceType.ToString(),
            MDToken = f.MDToken.ToInt32(),
        }).Select(t => JsonSerializer.Serialize(t)).ToArray();
        return types.Length > 0 ? paginate.Paginate(types,offset, pageSize) : new[] { "No data found." };
    }

    [McpServerTool, Description("Get detailed information about a specific type")]
    public static string GetTypeInfo(string typeName)
    {
        if (Module == null)
            return "No assembly loaded.";
        
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
    public static string[] FindStringLiterals(
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
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
                            MDToken = method.MDToken.ToInt32(),
                            RID = method.Rid,
                            StringLiteral = str,
                            InstrOffset = instr.Offset,
                        };
                        strings.Add(JsonSerializer.Serialize(data));
                    }
                }
            }
        }
        
        return paginate.Paginate(strings.ToArray(),offset, pageSize);
    }

    [McpServerTool, Description("Search for types full name containing a substring")]
    public static string[] SearchTypes(
        [Description("Substring to search for")]
        string substring,
        [Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var matchingTypes = Module.Types
            .Where(t => t.FullName.Contains(substring, StringComparison.OrdinalIgnoreCase))
            .Select(f => new
            {
                Name = f.Name.ToString(),
                FullName = f.FullName.ToString(),
                MDToken = f.MDToken.ToInt32(),
                Namespace = f.Namespace.ToString(),
            }).Select(t => JsonSerializer.Serialize(t)).ToArray();
            
        return matchingTypes.Length > 0 ? paginate.Paginate(matchingTypes,offset, pageSize) : new[] { $"No types matching '{substring}' found." };
    }

    [McpServerTool, Description("Examine constructor initialization for a type")]
    public static string[] ExamineConstructors(string typeName)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
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
        if (Module == null)
            return new[] { "No assembly loaded." };
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
    public static string[] FindMethodUsages(string methodName,
    [Description("Offset to start listing from(start at 0)")]
    int offset = 0,
    [Description("Number of items to list(100 is a good number,0 means remainder)")]
    int pageSize = 100
    )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
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
        
        return usages.Count > 0 ? paginate.Paginate(usages.ToArray(),offset, pageSize) : new[] { $"No usages of '{methodName}' found." };
    }

    [McpServerTool, Description("Find possible reflection usage in the assembly")]
    public static string[] FindReflectionUsage([Description("Offset to start listing from(start at 0)")]
        int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")]
        int pageSize = 100)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
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
        
        return results.Count > 0 ? paginate.Paginate(results.ToArray(),offset, pageSize) : new[] { "No reflection usage found." };
    }

    [McpServerTool, Description("Extract method control flow graph")]
    public static string[] ExtractControlFlowGraph(string methodName)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
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

    class MethodRawBody
    {
        public byte HeaderSize;
        public uint CodeSize;
        public byte[] Code;
        public uint MethodFileOffset;
        public uint MethodRVA;
        public uint CodeFileOffset;
        
    }
    
    static MethodRawBody GetRawILBytes(MethodDef method, ModuleDefMD module)
    {
        if (!method.HasBody || method.Body == null)
            return null;

        // Get the method's RVA
        var rva = method.RVA;
        if (rva == 0)
            return null;
        
        var output = new MethodRawBody();

        // Read the method body from the module's metadata
        var reader = module.Metadata.PEImage.CreateReader();
        reader.Position = (uint)module.Metadata.PEImage.ToFileOffset(rva);
        output.MethodFileOffset = reader.Position;
        output.MethodRVA = (uint)rva;

        // Read the method body header and IL code
        // Simplified: Assumes a "fat" header (most common for methods with IL)
        // For production code, handle tiny headers and other edge cases
        byte headerFlags = reader.ReadByte();
        int codeSize;
        if ((headerFlags & 0x03) == 0x02) // Tiny header
        {
            codeSize = headerFlags >> 2;
            output.HeaderSize = 1;
        }
        else if ((headerFlags & 0x03) == 0x03) // Fat header
        {
            reader.Position -= 1; // Rewind to read full header
            var fatHeader = reader.ReadBytes(12); // Fat header is 12 bytes
            codeSize = BitConverter.ToInt32(fatHeader, 4); // Code size at offset 4
            output.HeaderSize = 12;
        }
        else
        {
            return null; // Invalid header
        }
        output.CodeSize = (uint)codeSize;
        output.Code = reader.ReadBytes(codeSize);
        output.CodeFileOffset = output.MethodFileOffset+output.HeaderSize;

        // Read the IL code bytes
        return output;
    }
    
    // Dump IL code for a method by method name
    [McpServerTool, Description("Get IL codes for a method by name")]
    public static string[] GetMethodILBodyByName(string methodName)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
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
                    
                    // Get method file offset
                    var offset = Module.Metadata.PEImage.ToFileOffset(method.RVA);
                    
                    
                    // Get the raw IL bytes
                    var data = GetRawILBytes(method, Module);
                    if (data == null)
                    {
                        results.Add($"Failed to get raw IL bytes for {method.FullName}");
                        break;
                    }

                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        var instr = method.Body.Instructions[i];
                        var hexdump = "";
                        if (i+1 < method.Body.Instructions.Count)
                            hexdump =BitConverter.ToString(data.Code, (int)instr.Offset, (int)method.Body.Instructions[i+1].Offset-(int)instr.Offset).Replace("-", " ");
                        else
                            hexdump =BitConverter.ToString(data.Code, (int)instr.Offset).Replace("-", " ");
                        results.Add($"{(data.CodeFileOffset+instr.Offset).ToString("x")} {hexdump} IL_{instr.Offset:X4}: {instr}");
                    }

                    break;
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No methods matching '{methodName}' found." };
    }
    
    // Dump IL code for a method by method RID
    [McpServerTool, Description("Get IL codes for a method by RID")]
    public static string[] GetMethodILBodyByRID(
        [Description("RID value for method")] uint rid
        )
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        var results = new List<string>();

        var method = Module.ResolveMethod(rid);
        if (method == null)
            return new[] { $"Method with RID '{rid}' not found." };
        
        if (method.Body == null)
        {
            results.Add($"Method {method.FullName} has no body");
            return results.ToArray();
        }
        
        results.Add($"IL code for {method.FullName}:");
        
        // Get the raw IL bytes
        var data = GetRawILBytes(method, Module);
        if (data == null)
        {
            results.Add($"Failed to get raw IL bytes for {method.FullName}");
            return results.ToArray();
        }

        for (int i = 0; i < method.Body.Instructions.Count; i++)
        {
            var instr = method.Body.Instructions[i];
            var hexdump = "";
            if (i+1 < method.Body.Instructions.Count)
                hexdump =BitConverter.ToString(data.Code, (int)instr.Offset, (int)method.Body.Instructions[i+1].Offset-(int)instr.Offset).Replace("-", " ");
            else
                hexdump =BitConverter.ToString(data.Code, (int)instr.Offset).Replace("-", " ");
            results.Add($"{(data.CodeFileOffset+instr.Offset).ToString("x")} {hexdump} IL_{instr.Offset:X4}: {instr}");
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No methods with RID '{rid}' found." };
    }
    
    // Find references to a specific method token, return array of strings each string is a JSON object with the following properties: MethodName, FullName, MDToken, Parameters
    [McpServerTool, Description("Find references to a specific method")]
    public static string[] FindMethodReferences(int token)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var results = new List<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is IMethodDefOrRef methodRef && methodRef.MDToken.ToInt32() == token)
                    {
                        var data = new
                        {
                            MethodName = method.Name.ToString(),
                            FullName = method.FullName.ToString(),
                            MDToken = method.MDToken.ToInt32(),
                            Parameters = method.MethodSig.Params.Select(p => p.ToString()).ToArray(),
                        };
                        results.Add(JsonSerializer.Serialize(data));
                    }
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No references to method token '{token}' found." };
    }
    
    // Find references to a specific string, return array of strings each string is a JSON object with the following properties: MethodName, FullName, MDToken, Parameters
    [McpServerTool, Description("Find references to a specific string")]
    public static string[] FindStringReferences(string str)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };
        
        var results = new List<string>();
        
        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is string strRef && strRef.Contains(str))
                    {
                        var data = new
                        {
                            MethodName = method.Name.ToString(),
                            FullName = method.FullName.ToString(),
                            MDToken = method.MDToken.ToInt32(),
                            Parameters = method.MethodSig.Params.Select(p => p.ToString()).ToArray(),
                        };
                        results.Add(JsonSerializer.Serialize(data));
                    }
                }
            }
        }
        
        return results.Count > 0 ? results.ToArray() : new[] { $"No references to string '{str}' found." };
    }
    
    // Read memory data at, specified RVA and size
    [McpServerTool, Description("Read memory data at specified RVA with size")]
    public static string ReadMemoryData(
        [Description("RVA to read from")]
        uint rva,
        [Description("Size of data to read")]
        uint size)
    {
        if (Module == null)
            return "No assembly loaded.";
        
        var data = Module.ReadDataAt((RVA)rva, (int)size);
        if (data == null)
            return $"No data found at RVA {rva:X8} with size {size}.";
        
        return JsonSerializer.Serialize(data);
    }
    
    // Save assembly using dnlib
    [McpServerTool, Description("Save assembly to a file")]
    public static string SaveAssembly(
        [Description("Path to save the assembly")]
        string path,
        [Description("Whether to save in mixed mode")]
        bool mixedMode = false
        )
    {
        if (Module == null)
            return "No assembly loaded.";

        try
        {
            if (mixedMode)
            {
                //Save the assembly in mixed mode to the specified path and write options
                var options = new NativeModuleWriterOptions(Module, false)
                {
                    MetadataOptions =
                    {
                        Flags = MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack
                    },
                    Logger = DummyLogger.NoThrowInstance,
                    AddCheckSum = true,
                };
                Module.NativeWrite(path, options);
            }
            else
            {
                // Save the assembly to the specified path and write options
                var options = new ModuleWriterOptions(Module)
                {
                    MetadataOptions =
                    {
                        Flags = MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack
                    },
                    Logger = DummyLogger.NoThrowInstance,
                    AddCheckSum = true,
                };
                Module.Write(path, options);
            }
        }
        catch (Exception ex)
        {
            return $"Failed to save assembly: {ex.Message}";
        }

        return $"Assembly saved to {path}.";
    }
    
    // Patch method instruction
    [McpServerTool, Description("Patch an instruction of a method")]
    public static string UpdateMethodInstruction(
        [Description("Method RID to patch")]
        uint rid,
        [Description("Offset to update")]
        uint offset,
        [Description("new instruction to replace (e.g., 'nop')")]
        string newInstruction)
    {
        if (Module == null)
            return "No assembly loaded.";
        
        var method = Module.ResolveMethod(rid);
        if (method == null)
            return $"Method with RID '{rid}' not found.";
        
        if (method.Body == null)
            return $"Method {method.FullName} has no body";
        
        var instruction = method.Body.Instructions.FirstOrDefault(i => i.Offset == offset);
        if (instruction == null)
            return $"Instruction at offset {offset} not found in method {method.FullName}.";
        
        // Parse the new instruction
        Instruction ins = null;
        try
        {
            ins = parser.ParseSingleInstruction(newInstruction);
        }
        catch (Exception ex)
        {
            return $"Failed to parse new instruction: {ex.Message}";
        }

        if (ins == null )
            return $"Failed to parse new instruction: {newInstruction}";

        if (ins.OpCode.OperandType == OperandType.ShortInlineBrTarget)
        {
            var parts = ((string)ins.Operand).Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return $"Instruction at offset {offset} has an invalid format";
            var targetOffset = uint.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);
            var targetInstruction = method.Body.Instructions.FirstOrDefault(i => i.Offset == targetOffset);
            if (targetInstruction == null)
                return $"Target instruction at offset {targetOffset} not found in method {method.FullName}.";
            ins.Operand = targetInstruction;
        }
        
        // Replace the instruction
        var index = method.Body.Instructions.IndexOf(instruction);
        if (index < 0)
            return $"Instruction at offset {offset} not found in method {method.FullName}.";
        method.Body.Instructions[index] = ins;
        
        method.Body.UpdateInstructionOffsets();
        
        return $"Method {method.FullName} updated successfully.";
    }
    
    
    // Patch method instructions
    [McpServerTool, Description("Patch method instructions")]
    public static string UpdateMethodInstructions(
        [Description("Method RID to patch")]
        uint rid,
        [Description("Start offset to update")]
        uint offset,
        [Description("New instructions to replace, separated by new lines (e.g., 'nop\nnop')")]
        string newInstruction)
    {
        if (Module == null)
            return "No assembly loaded.";
        
        var method = Module.ResolveMethod(rid);
        if (method == null)
            return $"Method with RID '{rid}' not found.";
        
        if (method.Body == null && offset > 0)
            return $"Method {method.FullName} has no body";
        
        if (method.Body == null)
            method.Body = new CilBody();
        
        var instruction = method.Body.Instructions.FirstOrDefault(i => i.Offset == offset);
        if (instruction == null)
            return $"Instruction at offset {offset} not found in method {method.FullName}.";
        
        // Parse the new instruction
        List<Instruction> newInstructions = new List<Instruction>();
        try
        {
            newInstructions = parser.ParseInstructions(newInstruction);
        }
        catch (Exception ex)
        {
            return $"Failed to parse new instructions: {ex.Message}";
        }

        if (newInstructions == null || newInstructions.Count == 0)
            return $"Failed to parse new instruction: {newInstruction}";

        foreach (var ins in newInstructions)
        {
            if (ins.OpCode.OperandType == OperandType.ShortInlineBrTarget)
            {
                var parts = ((string)ins.Operand).Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    return $"Instruction at offset {offset} has an invalid format";
                var targetOffset = uint.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);
                var targetInstruction = method.Body.Instructions.FirstOrDefault(i => i.Offset == targetOffset);
                if (targetInstruction == null)
                    return $"Target instruction at offset {targetOffset} not found in method {method.FullName}.";
                ins.Operand = targetInstruction;
            }
        }
        
        // If the new instructions are longer than the original, clear the existing instructions
        if (newInstructions.Count > method.Body.Instructions.Count)
        {
            method.Body.Instructions.Clear();
            foreach (var ins in newInstructions)
            {
                method.Body.Instructions.Add(ins);
            }
        } 
        else
        {
            var i = method.Body.Instructions.IndexOf(instruction);
            if (i < 0)
                return $"Instruction at offset {offset} not found in method {method.FullName}.";
            
            // Check if the new instructions fit in the existing instructions
            if (i + newInstructions.Count > method.Body.Instructions.Count)
            {
                for (int j = 0; j < i + newInstructions.Count - method.Body.Instructions.Count; j++)
                {
                    // Add empty instructions to fill the gap
                    method.Body.Instructions.Add(new Instruction(OpCodes.Nop));
                }
            }
            
            // Replace the instructions start from the specified offset
            for (int j = i; j < i+newInstructions.Count; j++)
            {
                method.Body.Instructions[j] = newInstructions[j-i];
            }

        }
        
        method.Body.UpdateInstructionOffsets();
        
        return $"Method {method.FullName} update successfully.";
    }
    
    // Find all usages of a specified field
    [McpServerTool, Description("Find all usages of a specified field")]
    public static string[] FindFieldReferences(
        string fieldName,
        [Description("Offset to start listing from(start at 0)")] int offset = 0,
        [Description("Number of items to list(100 is a good number,0 means remainder)")] int pageSize = 100)
    {
        if (Module == null)
            return new[] { "No assembly loaded." };

        var references = new List<string>();

        foreach (var type in Module.Types)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;

                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.Operand is IField field &&
                        field.FullName.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        references.Add($"{method.FullName} accesses {field.FullName} at IL_{instr.Offset:X4}");
                    }
                }
            }
        }

        return references.Count > 0
            ? paginate.Paginate(references.ToArray(), offset, pageSize)
            : new[] { $"No references to field '{fieldName}' found." };
    }
}