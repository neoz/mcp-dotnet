namespace MCPPOC;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

public class InstructionParser
{
    private readonly ModuleDefMD _module;
    private readonly Dictionary<string, OpCode> _opcodeMap;

    public InstructionParser(ModuleDefMD module)
    {
        _module = module;
        _opcodeMap = InitializeOpcodeMap();
    }

    // Initialize a map of opcode names to dnlib OpCode objects
    private Dictionary<string, OpCode> InitializeOpcodeMap()
    {
        var map = new Dictionary<string, OpCode>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in typeof(OpCodes).GetFields())
        {
            if (field.FieldType == typeof(OpCode))
            {
                var opcode = (OpCode)field.GetValue(null);
                map[opcode.Name.Replace(".", "")] = opcode;
            }
        }

        return map;
    }

    // Parse a string of instructions into a list of dnlib Instructions
    public List<Instruction> ParseInstructions(string input)
    {
        var instructions = new List<Instruction>();
        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var instruction = ParseSingleInstruction(line.Trim());
            if (instruction != null)
                instructions.Add(instruction);
        }

        return instructions;
    }

    // Parse a single instruction line
    public Instruction ParseSingleInstruction(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (line.StartsWith("switch", StringComparison.OrdinalIgnoreCase))
        {
            parts = new string[2];
            // switch(IL_0018,IL_0021,IL_002A,IL_0033)
            parts[0] = "switch";
            parts[1] = line.Substring("switch".Length).Trim();
        }

        var opcodeName = parts[0];
        if (!_opcodeMap.TryGetValue(opcodeName, out var opcode))
        {
            // try remove . from opcode name 
            opcodeName = opcodeName.Replace(".", "");
            if (!_opcodeMap.TryGetValue(opcodeName, out opcode))
                throw new ArgumentException($"Unknown opcode: {opcodeName}");
        }


        object operand = null;
        if (parts.Length > 1)
        {
            var operandStr = string.Join(" ", parts.Skip(1)).Trim();
            operand = ParseOperand(opcode, operandStr);
        }

        return new Instruction(opcode, operand);
    }

    // Parse the operand based on the opcode and operand string
    internal object ParseOperand(OpCode opcode, string operandStr)
    {
        switch (opcode.OperandType)
        {
            case OperandType.InlineString:
                // Handle string literals (e.g., ldstr "Hello")
                if (operandStr.StartsWith("\"") && operandStr.EndsWith("\""))
                    return operandStr.Substring(1, operandStr.Length - 2);
                throw new ArgumentException($"Invalid string operand: {operandStr}");

            case OperandType.InlineI:
            case OperandType.ShortInlineI:
                // Handle integer literals (e.g., ldc.i4 42)
                if (int.TryParse(operandStr, out var intValue))
                    return intValue;
                throw new ArgumentException($"Invalid integer operand: {operandStr}");

            case OperandType.InlineMethod:
                // Handle method calls (e.g., call System.Console::WriteLine)
                return ResolveMethod(operandStr);

            case OperandType.InlineType:
                // Handle type references (e.g., newobj System.Object::.ctor)
                return ResolveType(operandStr);

            case OperandType.InlineNone:
                // No operand expected
                if (!string.IsNullOrEmpty(operandStr))
                    throw new ArgumentException($"Opcode {opcode.Name} does not expect an operand");
                return null;
            case OperandType.InlineI8:
                // Handle long integer literals (e.g., ldc.i8 42L)
                if (operandStr.EndsWith("L", StringComparison.OrdinalIgnoreCase))
                    operandStr = operandStr.Substring(0, operandStr.Length - 1);
                if (long.TryParse(operandStr, out var longValue))
                    return longValue;
                throw new ArgumentException($"Invalid long integer operand: {operandStr}");

            case OperandType.InlineR:
                // Handle floating point literals (e.g., ldc.r8 3.14)
                if (double.TryParse(operandStr, out var doubleValue))
                    return doubleValue;
                throw new ArgumentException($"Invalid floating point operand: {operandStr}");

            case OperandType.ShortInlineR:
                // Handle single precision floating point literals (e.g., ldc.r4 3.14)
                if (float.TryParse(operandStr, out var floatValue))
                    return floatValue;
                throw new ArgumentException($"Invalid single precision floating point operand: {operandStr}");
            
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                // Handle branch targets (e.g., br IL_0010)
                if (operandStr.StartsWith("IL_", StringComparison.OrdinalIgnoreCase))
                    return operandStr; // Return as string, will be resolved later
                throw new ArgumentException($"Invalid branch target: {operandStr}");

            case OperandType.InlineVar:
            case OperandType.ShortInlineVar:
                // Handle local variables or arguments (e.g., ldloc.0, ldarg.1)
                if (int.TryParse(operandStr, out var varIndex))
                    return varIndex;
                
                ResolveParameter(operandStr);
                
                return operandStr;
                //throw new ArgumentException($"Invalid variable index: {operandStr}");
            
            case OperandType.InlineSwitch:
                // Handle switch targets (e.g., switch (IL_0010, IL_0020, IL_0030))
                if (operandStr.StartsWith("(") && operandStr.EndsWith(")"))
                {
                    var targets = operandStr.Substring(1, operandStr.Length - 2)
                        .Split(',')
                        .Select(t => t.Trim())
                        .ToArray();
                    return targets;
                }
                throw new ArgumentException($"Invalid switch targets: {operandStr}");

            case OperandType.InlineTok:
                // Handle token references which could be types, methods or fields
                if (operandStr.Contains("::"))
                {
                    if (operandStr.Contains("("))
                        return ResolveMethod(operandStr); // Method reference
                    return ResolveField(operandStr); // Field reference
                }
                return ResolveType(operandStr); // Type reference
            
            // Add more operand types as needed
            default:
                return operandStr;
            //throw new NotSupportedException($"Operand type {opcode.OperandType} not supported yet");
        }
    }
    
    public object ResolveParameter(string operandStr, MethodDef method = null)
    {
        // Check if it's a local variable reference (ldloc, stloc)
        if (operandStr.StartsWith("V_", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(operandStr.Substring(2), out var localIndex))
        {
            return localIndex;
        }

        // Check if it's an argument reference (ldarg, starg)
        if (operandStr.StartsWith("A_", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(operandStr.Substring(2), out var argIndex))
        {
            return argIndex;
        }

        // For named locals and parameters, try to resolve them if method context is provided
        if (method != null)
        {
            // Check if it's a named local variable
            if (method.Body?.Variables != null)
            {
                for (int i = 0; i < method.Body.Variables.Count; i++)
                {
                    var local = method.Body.Variables[i];
                    if (local.Name == operandStr)
                    {
                        return i; // Return the index of the local variable
                    }
                }
            }

            // Check if it's a named parameter
            if (method.Parameters != null)
            {
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var param = method.Parameters[i];
                    if (param.Name == operandStr)
                    {
                        return i; // Return the index of the parameter
                    }
                }
            }
        }

        // If we couldn't resolve the name, return the string for later resolution
        return operandStr;
    }
    
    private IField ResolveField(string fieldStr)
{
    try
    {
        // Extract assembly name if present
        string asmName = null;
        if (fieldStr.Contains('[') && fieldStr.Contains(']'))
        {
            int startIndex = fieldStr.IndexOf('[') + 1;
            int endIndex = fieldStr.IndexOf(']');
            if (startIndex < endIndex)
            {
                asmName = fieldStr.Substring(startIndex, endIndex - startIndex);
                fieldStr = fieldStr.Replace($"[{asmName}]", "");
            }
        }

        // Check for static/instance field indicator and remove it
        if (fieldStr.StartsWith("instance "))
            fieldStr = fieldStr.Substring("instance ".Length);
        else if (fieldStr.StartsWith("static "))
            fieldStr = fieldStr.Substring("static ".Length);

        // Extract field type and remaining parts
        int spaceIndex = fieldStr.IndexOf(' ');
        if (spaceIndex <= 0)
            throw new ArgumentException($"Invalid field format: {fieldStr}");

        string fieldType = fieldStr.Substring(0, spaceIndex);
        fieldStr = fieldStr.Substring(spaceIndex + 1);

        // Split by :: to get type and field name
        var parts = fieldStr.Split(new[] { "::" }, StringSplitOptions.None);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid field format: {fieldStr}");

        string typeName = parts[0].Trim();
        string fieldName = parts[1].Trim();

        // Get type reference
        ITypeDefOrRef typeRef;
        if (asmName != null)
        {
            // Try to find the assembly reference
            var asmRef = _module.GetAssemblyRefs()
                .FirstOrDefault(a => a.Name == asmName || a.FullName == asmName);

            if (asmRef == null)
                throw new ArgumentException($"Assembly reference not found: {asmName}");

            // Create type reference with assembly reference
            typeRef = new TypeRefUser(_module, typeName.Substring(typeName.LastIndexOf('.') + 1),
                typeName.Substring(0, typeName.LastIndexOf('.')),
                new AssemblyRefUser(asmRef));
        }
        else
        {
            // Look in the current module
            typeRef = _module.Find(typeName, true);
        }

        if (typeRef == null)
        {
            // Fallback to TypeRefUser if not found
            int lastDot = typeName.LastIndexOf('.');
            string ns = lastDot > 0 ? typeName.Substring(0, lastDot) : "";
            string name = lastDot > 0 ? typeName.Substring(lastDot + 1) : typeName;
            typeRef = new TypeRefUser(_module, name, ns);
        }

        // Create field signature
        var fieldTypeSig = ResolveTypeSignature(fieldType);

        // Create field reference
        return new MemberRefUser(
            _module,
            fieldName,
            new FieldSig(fieldTypeSig),
            typeRef);
    }
    catch (Exception ex)
    {
        throw new ArgumentException($"Failed to resolve field: {fieldStr}", ex);
    }
}

    // Resolve a method reference from a string (e.g., "System.Console::WriteLine")
    // Resolve a method reference from a string
    private IMethod ResolveMethod(string methodStr)
    {
        try
        {
            // Handle more complex method signatures like:
            // "instance void [System.Runtime]System.DateTime::.ctor(int32, int32, int32)"

            // Extract assembly name if present
            string asmName = null;
            if (methodStr.Contains('[') && methodStr.Contains(']'))
            {
                int startIndex = methodStr.IndexOf('[') + 1;
                int endIndex = methodStr.IndexOf(']');
                if (startIndex < endIndex)
                {
                    asmName = methodStr.Substring(startIndex, endIndex - startIndex);
                    methodStr = methodStr.Replace($"[{asmName}]", "");
                }
            }

            // Check for instance/static method indicator
            bool isInstance = false;
            if (methodStr.StartsWith("instance "))
            {
                isInstance = true;
                methodStr = methodStr.Substring("instance ".Length);
            }
            else if (methodStr.StartsWith("static "))
            {
                methodStr = methodStr.Substring("static ".Length);
            }

            // Extract return type and remaining parts
            int spaceIndex = methodStr.IndexOf(' ');
            if (spaceIndex <= 0)
                throw new ArgumentException($"Invalid method format: {methodStr}");

            string returnType = methodStr.Substring(0, spaceIndex);
            methodStr = methodStr.Substring(spaceIndex + 1);

            // Split by :: to get type and method name with params
            var parts = methodStr.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid method format: {methodStr}");

            string typeName = parts[0].Trim();
            string methodNameWithParams = parts[1].Trim();

            // Extract method name and parameters
            int parenIndex = methodNameWithParams.IndexOf('(');
            if (parenIndex < 0)
                throw new ArgumentException($"Invalid method name format: {methodNameWithParams}");

            string methodName = methodNameWithParams.Substring(0, parenIndex);
            string paramsStr = methodNameWithParams.Substring(parenIndex + 1).TrimEnd(')');

            // Get type reference
            ITypeDefOrRef typeRef;
            if (asmName != null)
            {
                // Try to find the assembly reference
                var asmRef = _module.GetAssemblyRefs()
                    .FirstOrDefault(a => a.Name == asmName || a.FullName == asmName);

                if (asmRef == null)
                    throw new ArgumentException($"Assembly reference not found: {asmName}");

                // Create type reference with assembly reference
                typeRef = new TypeRefUser(_module, typeName.Substring(typeName.LastIndexOf('.') + 1),
                    typeName.Substring(0, typeName.LastIndexOf('.')),
                    new AssemblyRefUser(asmRef));
            }
            else
            {
                // Look in the current module
                typeRef = _module.Find(typeName, true);
            }

            if (typeRef == null)
            {
                // fallback to TypeRefUser if not found
                typeRef = new TypeRefUser(_module, typeName.Substring(typeName.LastIndexOf('.') + 1),
                    typeName.Substring(0, typeName.LastIndexOf('.')));
            }
            
            if (typeRef == null)
                throw new ArgumentException($"Type not found: {typeName}");

            // Parse parameters
            var parameters = new List<TypeSig>();
            if (!string.IsNullOrWhiteSpace(paramsStr))
            {
                var paramTypes = paramsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var paramType in paramTypes)
                {
                    parameters.Add(ResolveTypeSignature(paramType.Trim()));
                }
            }

            // Create method reference
            var returnTypeSig = ResolveTypeSignature(returnType);

            return new MemberRefUser(
                _module,
                methodName,
                MethodSig.CreateInstance(returnTypeSig, parameters.ToArray()),
                typeRef);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to resolve method: {methodStr}", ex);
        }
    }

// Helper method to resolve type signatures like "int32", "string", etc.
    private TypeSig ResolveTypeSignature(string typeSignature)
    {
        switch (typeSignature.ToLowerInvariant())
        {
            case "void": return _module.CorLibTypes.Void;
            case "bool": return _module.CorLibTypes.Boolean;
            case "char": return _module.CorLibTypes.Char;
            case "int8":
            case "sbyte": return _module.CorLibTypes.SByte;
            case "uint8":
            case "byte": return _module.CorLibTypes.Byte;
            case "int16":
            case "short": return _module.CorLibTypes.Int16;
            case "uint16":
            case "ushort": return _module.CorLibTypes.UInt16;
            case "int32":
            case "int": return _module.CorLibTypes.Int32;
            case "uint32":
            case "uint": return _module.CorLibTypes.UInt32;
            case "int64":
            case "long": return _module.CorLibTypes.Int64;
            case "uint64":
            case "ulong": return _module.CorLibTypes.UInt64;
            case "float32":
            case "single":
            case "float": return _module.CorLibTypes.Single;
            case "float64":
            case "double": return _module.CorLibTypes.Double;
            case "string": return _module.CorLibTypes.String;
            case "object": return _module.CorLibTypes.Object;
            default:
                // Try to resolve as a custom type
                var type = _module.Find(typeSignature, true);
                if (type != null)
                    return type.ToTypeSig();
                else
                {
                    ITypeDefOrRef typeRef = _module.CorLibTypes.GetTypeRef("System", typeSignature);
                    if (typeRef != null)
                        return typeRef.ToTypeSig();
                    
                    // For custom types, parse namespace and name
                    string ns = typeSignature.Contains(".") 
                        ? typeSignature.Substring(0, typeSignature.LastIndexOf('.')) 
                        : "";
                    string name = typeSignature.Contains(".") 
                        ? typeSignature.Substring(typeSignature.LastIndexOf('.') + 1) 
                        : typeSignature;
            
                    typeRef = new TypeRefUser(_module, name, ns);
                    return typeRef.ToTypeSig();
                }

                //throw new ArgumentException($"Unsupported type signature: {typeSignature}");
        }
    }

    // Resolve a type reference from a string (e.g., "System.Object")
    private ITypeDefOrRef ResolveType(string typeStr)
    {
        ITypeDefOrRef typeRef = _module.CorLibTypes.GetTypeRef("System", typeStr);
        if (typeRef != null)
            return typeRef;
        
        var type = _module.Find(typeStr, true);
        if (type == null)
            throw new ArgumentException($"Type not found: {typeStr}");

        return _module.Import(type);
    }
}

// Example usage
// public class Program
// {
//     public static void Main()
//     {
//         // Load a module (e.g., the current assembly)
//         var module = ModuleDefMD.Load(typeof(Program).Assembly.Location);
//
//         var parser = new InstructionParser(module);
//
//         // Example input string
//         var input = @"
//             ldstr ""Hello, World!""
//             call System.Console::WriteLine
//             ret
//         ";
//
//         try
//         {
//             var instructions = parser.ParseInstructions(input);
//             foreach (var instr in instructions)
//             {
//                 Console.WriteLine($"Parsed: {instr}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Error: {ex.Message}");
//         }
//     }
// }