using System.Reflection;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MCPPOC;
using Xunit;

namespace MCPPOC;

public class InstructionParserTest
{
    private const string _testAssemblyPath = "../../../mcp_example/dnlib.dll"; // Path to the test assembly

    [Fact]
    public void ParseInstructionsTest()
    {
        //Load a module (e.g., the current assembly)
        var module = ModuleDefMD.Load(typeof(Program).Assembly.Location);

        var parser = new InstructionParser(module);

        // Example input string
        var input = @"
            ldstr ""Hello, World!""
            call System.Console::WriteLine
            ret
        ";

        try
        {
            var instructions = parser.ParseInstructions(input);
            foreach (var instr in instructions)
            {
                Console.WriteLine($"Parsed: {instr}");
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    
    [Fact]
    public void ParseOperatorTest()
    {
        // Load reference assembly module for resolving types
        // var assemblyPath = typeof(Program).Assembly.Location;
        // var resolver = new AssemblyResolver();
        // var moduleContext = new ModuleContext(resolver);
        //
        // // Create an assembly resolver that can find referenced assemblies
        // resolver.DefaultModuleContext = moduleContext;
        // resolver.EnableTypeDefCache = true;
        //
        // // Load the main module
        // var module = ModuleDefMD.Load(assemblyPath, moduleContext);
        // resolver.AddToCache(module);
        //
        // // Load referenced assemblies
        // foreach (var asmRef in module.GetAssemblyRefs())
        // {
        //     try
        //     {
        //         // Resolve and load the referenced assembly
        //         var assembly = resolver.Resolve(asmRef, module);
        //         if (assembly != null)
        //         {
        //             Console.WriteLine($"Loaded reference: {asmRef.Name}");
        //         }
        //         
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Failed to load assembly {asmRef.Name}: {ex.Message}");
        //     }
        // }
        var module = ModuleDefMD.Load(typeof(Program).Assembly.Location);
        
        // var consoleType = new TypeRefUser(module, "System", "DateTime");
        // var type = module.Find("System.Console", true);
        // if (type == null)
        //     throw new ArgumentException($"Type not found");


        var parser = new InstructionParser(module);
        
        var input = "instance void [System.Runtime]System.DateTime::.ctor(int32, int32, int32)";
        var opcode = dnlib.DotNet.Emit.OpCodes.Newobj;
        var result = parser.ParseOperand(opcode, input);
        Assert.NotNull(result);
    }
    
    [Fact]
    public void ParseInstructions_FullTest()
    {
        var module = ModuleDefMD.Load(_testAssemblyPath);

        foreach (var type in module.GetTypes())
        {
            // Loop all methods in the type
            foreach (var method in type.Methods)
            {
                // Check if the method has a body
                if (method.HasBody)
                {
                    // Get the IL instructions
                    var ilInstructions = method.Body.Instructions;

                    // Create an InstructionParser instance
                    var parser = new InstructionParser(module);
                    
                    // Parse the instructions
                    foreach (var instruction in ilInstructions)
                    {
                        try
                        {
                            var s = $"{instruction.ToString()}";
                            s = s.Substring(s.IndexOf(" ", StringComparison.Ordinal) + 1);
                            var parsedInstruction = parser.ParseSingleInstruction(s);
                            //Console.WriteLine($"Parsed instruction: {parsedInstruction}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing instruction: {instruction.ToString()} {ex.Message}");
                        }
                    }
                }
            }
        }
        
    }
}