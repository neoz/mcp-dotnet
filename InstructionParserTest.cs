using System.Reflection;
using System.Reflection.Emit;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using MCPPOC;
using Xunit;
using Xunit.Abstractions;

namespace MCPPOC;

public class InstructionParserTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public InstructionParserTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private const string _testAssemblyPath = "../../../mcp_example/dnlib.dll"; // Path to the test assembly
    
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
                            //_testOutputHelper.WriteLine($"Parsed instruction: {parsedInstruction}");
                        }
                        catch (Exception ex)
                        {
                            _testOutputHelper.WriteLine($"Error parsing instruction: {instruction.ToString()} {ex.Message}");
                        }
                    }
                }
            }
        }
        
    }
}