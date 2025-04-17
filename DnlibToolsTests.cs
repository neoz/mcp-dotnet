using dnlib.DotNet;
using Xunit;

public class DnlibToolsTests
{
    [Fact]
    public void GetMethodILBodyByName_ReturnsCorrectIL()
    {
        // Arrange
        string testAssemblyPath = "C:\\working\\ida\\mcp-reversing-dataset\\dotnet\\ConsoleApp1.dll"; // Replace with a valid test assembly path
        string methodName = "<Main>$"; // Replace with a known method name in the test assembly
        DnlibTools.LoadAssembly(testAssemblyPath);

        // Act
        string[] result = DnlibTools.GetMethodILBodyByName(methodName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}