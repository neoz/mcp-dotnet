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
    private object ParseOperand(OpCode opcode, string operandStr)
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
    private IMethod ResolveMethod(string methodStr)
    {
        // Simplified parsing: assumes format "Namespace.Type::Method"
        var parts = methodStr.Split(new[] { "::" }, StringSplitOptions.None);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid method format: {methodStr}");

        var typeName = parts[0];
        var methodName = parts[1];

        var type = _module.Find(typeName, true);
        if (type == null)
            throw new ArgumentException($"Type not found: {typeName}");

        // Find the method (simplified, assumes no overloads or parameters for brevity)
        var method = type.FindMethod(methodName);
        if (method == null)
            throw new ArgumentException($"Method not found: {methodName} in {typeName}");

        return _module.Import(method);
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