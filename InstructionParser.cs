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
                return operandStr;

            // Add more operand types as needed
            default:
                return operandStr;
            //throw new NotSupportedException($"Operand type {opcode.OperandType} not supported yet");
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
                if (typeRef == null)
                    throw new ArgumentException($"Type not found: {typeName}");
            }

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

                throw new ArgumentException($"Unsupported type signature: {typeSignature}");
        }
    }

    // Resolve a type reference from a string (e.g., "System.Object")
    private ITypeDefOrRef ResolveType(string typeStr)
    {
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