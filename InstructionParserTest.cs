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
}