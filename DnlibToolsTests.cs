using System.Text.Json;
using dnlib.DotNet;
using Xunit;

public class DnlibToolsTests
{
    static string _testAssemblyPath = "C:\\working\\ida\\mcp-reversing-dataset\\dotnet\\ConsoleApp1.dll"; // Replace with a valid test assembly path
    static string _methodName = "<Main>$"; // Replace with a known method name in the test assembly
    static string _typeName = "Program"; // Sample type name

    [Fact]
    public void LoadAssembly_LoadsValidAssembly()
    {
        // Act
        bool result = DnlibTools.LoadAssembly(_testAssemblyPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(DnlibTools.Module);
    }

    [Fact]
    public void GetEntryPoint_ReturnsEntryPointInfo()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string result = DnlibTools.GetEntryPoint();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("No assembly loaded", result);
        Assert.DoesNotContain("No entry point found", result);
    }

    [Fact]
    public void ListTypes_ReturnsTypesFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListTypes();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain("No assembly loaded.", result);
        Assert.DoesNotContain("No types found.", result);
    }

    [Fact]
    public void ListTypesRegex_ReturnsMatchingTypes()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Program";

        // Act
        string[] result = DnlibTools.ListTypesRegex(pattern);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain($"No types matching '{pattern}' found.", result);
    }

    [Fact]
    public void ListMethods_ReturnsMethodsForType()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListMethods(_typeName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain($"Type '{_typeName}' not found.", result);
        Assert.DoesNotContain($"No methods found for '{_typeName}'.", result);
    }

    [Fact]
    public void FindMethodsWithRegex_ReturnsMatchingMethods()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Main";

        // Act
        string[] result = DnlibTools.FindMethodsWithRegex(pattern);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain($"No methods matching '{pattern}' found.", result);
    }

    [Fact]
    public void ListFields_ReturnsFieldsFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListFields();

        // Assert
        Assert.NotNull(result);
        // Note: This assertion depends on whether the test assembly has fields
    }

    [Fact]
    public void ListProperties_ReturnsPropertiesFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListProperties();

        // Assert
        Assert.NotNull(result);
        // Note: This assertion depends on whether the test assembly has properties
    }

    [Fact]
    public void ListEvents_ReturnsEventsFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListEvents();

        // Assert
        Assert.NotNull(result);
        // Note: This assertion depends on whether the test assembly has events
    }

    [Fact]
    public void ListResources_ReturnsResourcesFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListResources();

        // Assert
        Assert.NotNull(result);
        // Note: This assertion depends on whether the test assembly has resources
    }

    [Fact]
    public void GetTypeInfo_ReturnsTypeInformation()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string result = DnlibTools.GetTypeInfo(_typeName);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain($"Type '{_typeName}' not found.", result);
        Assert.Contains("Name:", result);
    }

    [Fact]
    public void FindStringLiterals_ReturnsStringsFromAssembly()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.FindStringLiterals();

        // Assert
        Assert.NotNull(result);
        // A typical .NET assembly should have at least some string literals
        Assert.NotEmpty(result);
    }

    [Fact]
    public void SearchTypes_ReturnsMatchingTypes()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Program";

        // Act
        string[] result = DnlibTools.SearchTypes(pattern);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void ExamineConstructors_ReturnsConstructorInfo()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ExamineConstructors(_typeName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain($"Type '{_typeName}' not found.", result);
    }

    [Fact]
    public void ListTypeDependencies_ReturnsDependenciesForType()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListTypeDependencies(_typeName);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain($"Type '{_typeName}' not found.", result);
    }

    [Fact]
    public void FindMethodUsages_ReturnsMethodUsages()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string method = "WriteLine"; // Common method likely to be called

        // Act
        string[] result = DnlibTools.FindMethodUsages(method);

        // Assert
        Assert.NotNull(result);
        // This will depend on whether the test assembly actually uses this method
    }

    [Fact]
    public void FindReflectionUsage_ReturnsReflectionUsages()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.FindReflectionUsage();

        // Assert
        Assert.NotNull(result);
        // This will depend on whether the test assembly uses reflection
    }

    [Fact]
    public void ExtractControlFlowGraph_ReturnsControlFlowGraph()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ExtractControlFlowGraph(_methodName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetMethodILBodyByName_ReturnsCorrectIL()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.GetMethodILBodyByName(_methodName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetMethodILBodyByRID_ReturnsCorrectIL()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        // First find a valid RID by getting a method
        var methodInfo = JsonSerializer.Deserialize<dynamic>(DnlibTools.FindMethodsWithRegex("Main")[0]);
        uint rid = (uint)methodInfo.GetProperty("RID").GetInt64();

        // Act
        string[] result = DnlibTools.GetMethodILBodyByRID(rid);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void FindMethodReferences_ReturnsMethodReferences()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        // First find a valid token by getting a method
        var methodInfo = JsonSerializer.Deserialize<dynamic>(DnlibTools.FindMethodsWithRegex("Encrypt")[0]);
        int token = (int)methodInfo.GetProperty("MDToken").GetInt64();

        // Act
        string[] result = DnlibTools.FindMethodReferences(token);

        // Assert
        Assert.NotNull(result);
        // This depends on whether the method is actually referenced anywhere
    }

    [Fact]
    public void FindStringReferences_ReturnsStringReferences()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Hello"; // Common string likely to be in the assembly

        // Act
        string[] result = DnlibTools.FindStringReferences(pattern);

        // Assert
        Assert.NotNull(result);
        // This depends on whether the string is actually used anywhere
    }

    [Fact]
    public void ReadMemoryData_ReturnsMemoryData()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        uint rva = 0x2000; // Sample RVA, adjust for your assembly
        uint size = 16;    // Small sample size

        // Act
        string result = DnlibTools.ReadMemoryData(rva, size);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain($"No data found at RVA {rva:X8} with size {size}.", result);
    }
}