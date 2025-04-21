using System.Reflection;
using System.Reflection.Emit;
using dnlib.DotNet;
using MCPPOC;
using Xunit;

namespace MCPPOC;

public class InstructionParserTest
{

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
    public void ParseInstructionsTest_2()
    {
        var module = ModuleDefMD.Load(typeof(Program).Assembly.Location);

        var parser = new InstructionParser(module);

        // Example input string
        var input = @"
            ldc.i4 2055
            ldc.i4.1
            ldc.i4.1
            newobj instance void [System.Runtime]System.DateTime::.ctor(int32, int32, int32)
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
}