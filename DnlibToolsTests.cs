using System;
using System.Text.Json;
using dnlib.DotNet;
using Xunit;

public class DnlibToolsTests
{
    private const string _testAssemblyPath = "../../../mcp_example/ConsoleApplication1.exe"; // Path to the test assembly
    private const string _methodName = "<Main>$"; 
    private const string _typeName = "Program"; 

    [Fact]
    public void LoadAssembly_LoadsValidAssembly()
    {
        // Act
        DnlibTools.LoadAssembly(_testAssemblyPath);

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
        string[] allTypes = DnlibTools.ListTypes(0, 0);
        string[] firstType = DnlibTools.ListTypes(0, 1);
        string[] secondPage = DnlibTools.ListTypes(1, 1);

        // Assert
        Assert.NotNull(allTypes);
        Assert.NotEmpty(allTypes);
        Assert.NotNull(firstType);
        Assert.Single(firstType);
        Assert.True(allTypes.Length > firstType.Length);
        
        // If there's more than one type, second page should return data
        if (allTypes.Length > 1)
        {
            Assert.NotEmpty(secondPage);
            Assert.NotEqual(firstType[0], secondPage[0]);
        }
    }

    [Fact]
    public void ListTypesRegex_ReturnsMatchingTypes()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Program";

        // Act
        string[] result = DnlibTools.ListTypesRegex(pattern);
        string[] nonExistingResult = DnlibTools.ListTypesRegex("NonExistingTypeXYZ123");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Contains(pattern));
        
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No types matching 'NonExistingTypeXYZ123' found.", nonExistingResult);
    }

    [Fact]
    public void ListMethods_ReturnsMethodsForType()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListMethods(_typeName);
        string[] nonExistingResult = DnlibTools.ListMethods("NonExistingType");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"Type 'NonExistingType' not found.", nonExistingResult);
    }

    [Fact]
    public void FindMethodsWithRegex_ReturnsMatchingMethods()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Main";

        // Act
        string[] result = DnlibTools.FindMethodsWithRegex(pattern);
        string[] nonExistingResult = DnlibTools.FindMethodsWithRegex("NonExistingMethodXYZ123");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.True(JsonSerializer.Deserialize<JsonElement>(r).TryGetProperty("Name", out _)));
        
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No methods matching 'NonExistingMethodXYZ123' found.", nonExistingResult);
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
        if (result.Length > 1 || !result[0].Contains("No fields found"))
        {
            Assert.All(result, r => Assert.True(JsonSerializer.Deserialize<JsonElement>(r).TryGetProperty("Name", out _)));
        }
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
        if (result.Length > 1 || !result[0].Contains("No properties found"))
        {
            Assert.All(result, r => Assert.True(JsonSerializer.Deserialize<JsonElement>(r).TryGetProperty("Name", out _)));
        }
    }

    [Fact]
    public void GetTypeInfo_ReturnsTypeInformation()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string result = DnlibTools.GetTypeInfo(_typeName);
        string nonExistingResult = DnlibTools.GetTypeInfo("NonExistingType");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Name:", result);
        Assert.Contains("Namespace:", result);
        
        Assert.NotNull(nonExistingResult);
        Assert.Contains($"Type 'NonExistingType' not found.", nonExistingResult);
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
        if (result.Length > 1 || !result[0].Contains("No string literals found"))
        {
            Assert.All(result, r => Assert.True(JsonSerializer.Deserialize<JsonElement>(r).TryGetProperty("StringLiteral", out _)));
        }
    }

    [Fact]
    public void SearchTypes_ReturnsMatchingTypes()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string pattern = "Program";

        // Act
        string[] result = DnlibTools.SearchTypes(pattern);
        string[] nonExistingResult = DnlibTools.SearchTypes("NonExistingTypeXYZ123");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Contains(pattern));
        
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No types matching 'NonExistingTypeXYZ123' found.", nonExistingResult);
    }

    [Fact]
    public void ExamineConstructors_ReturnsConstructorInfo()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ExamineConstructors(_typeName);
        string[] nonExistingResult = DnlibTools.ExamineConstructors("NonExistingType");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"Type 'NonExistingType' not found.", nonExistingResult);
    }

    [Fact]
    public void ListTypeDependencies_ReturnsDependenciesForType()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ListTypeDependencies(_typeName);
        string[] nonExistingResult = DnlibTools.ListTypeDependencies("NonExistingType");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"Type 'NonExistingType' not found.", nonExistingResult);
    }

    [Fact]
    public void FindMethodUsages_ReturnsMethodUsages()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        string method = "WriteLine"; // Common method likely to be called

        // Act
        string[] result = DnlibTools.FindMethodUsages(method);
        string[] nonExistingResult = DnlibTools.FindMethodUsages("NonExistingMethodXYZ123");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        if (nonExistingResult.Length == 1)
        {
            Assert.Contains($"No usages of 'NonExistingMethodXYZ123' found.", nonExistingResult);
        }
    }

    [Fact]
    public void ExtractControlFlowGraph_ReturnsControlFlowGraph()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.ExtractControlFlowGraph(_methodName);
        string[] nonExistingResult = DnlibTools.ExtractControlFlowGraph("NonExistingMethod");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        if (nonExistingResult.Length == 1)
        {
            Assert.Contains($"No methods matching 'NonExistingMethod' found.", nonExistingResult);
        }
    }

    [Fact]
    public void GetMethodILBodyByName_ReturnsCorrectIL()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);

        // Act
        string[] result = DnlibTools.GetMethodILBodyByName(_methodName);
        string[] nonExistingResult = DnlibTools.GetMethodILBodyByName("NonExistingMethod");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No methods matching 'NonExistingMethod' found.", nonExistingResult);
    }

    [Fact]
    public void GetMethodILBodyByRID_ReturnsCorrectIL()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        // First find a valid method with RID
        string[] methods = DnlibTools.FindMethodsWithRegex("Main");
        Assert.NotEmpty(methods);
        
        JsonElement methodInfo = JsonSerializer.Deserialize<JsonElement>(methods[0]);
        uint rid = (uint)methodInfo.GetProperty("RID").GetInt64();

        // Act
        string[] result = DnlibTools.GetMethodILBodyByRID(rid);
        string[] nonExistingResult = DnlibTools.GetMethodILBodyByRID(999999); // Use an unlikely to exist RID

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        Assert.NotNull(nonExistingResult);
        if (nonExistingResult.Length == 1)
        {
            Assert.Contains($"Method with RID '999999' not found.", nonExistingResult);
        }
    }

    [Fact]
    public void FindMethodReferences_ReturnsMethodReferences()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        // Try to find a method token from any method that might be referenced
        string[] methods = DnlibTools.FindMethodsWithRegex("Console");
        if (methods.Length == 0 || methods[0].Contains("No methods matching"))
        {
            // Skip test if we can't find a good method
            return;
        }
        
        JsonElement methodInfo = JsonSerializer.Deserialize<JsonElement>(methods[0]);
        int token = (int)methodInfo.GetProperty("MDToken").GetInt64();

        // Act
        string[] result = DnlibTools.FindMethodReferences(token);
        string[] nonExistingResult = DnlibTools.FindMethodReferences(999999999); // Use an unlikely to exist token

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No references to method token '999999999' found.", nonExistingResult);
    }

    [Fact]
    public void FindStringReferences_ReturnsStringReferences()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        // First find a string literal that actually exists
        string[] literals = DnlibTools.FindStringLiterals();
        string searchString = "Hello";
        
        if (literals.Length > 0 && !literals[0].Contains("No string literals found"))
        {
            JsonElement firstLiteral = JsonSerializer.Deserialize<JsonElement>(literals[0]);
            if (firstLiteral.TryGetProperty("Value", out JsonElement valueElement))
            {
                searchString = valueElement.GetString() ?? "Hello";
                if (searchString.Length > 5)
                {
                    searchString = searchString.Substring(0, 5);
                }
            }
        }

        // Act
        string[] result = DnlibTools.FindStringReferences(searchString);
        string[] nonExistingResult = DnlibTools.FindStringReferences("XYZABC123ThisStringDoesNotExist");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(nonExistingResult);
        Assert.Single(nonExistingResult);
        Assert.Contains($"No references to string 'XYZABC123ThisStringDoesNotExist' found.", nonExistingResult);
    }

    [Fact]
    public void ReadMemoryData_ReturnsMemoryData()
    {
        // Arrange
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        // Get entry point RVA as a valid starting point
        string epInfo = DnlibTools.GetEntryPoint();
        if (string.IsNullOrEmpty(epInfo) || epInfo.Contains("No entry point found"))
        {
            // Skip test if no entry point
            return;
        }
        
        // Parse the RVA from the entry point info
        string[] parts = epInfo.Split(',');
        uint rva = 0x2000; // Default value
        
        foreach (string part in parts)
        {
            if (part.Contains("RVA:"))
            {
                string rvaStr = part.Split(':')[1].Trim();
                if (rvaStr.StartsWith("0x"))
                {
                    rvaStr = rvaStr.Substring(2);
                }
                
                if (uint.TryParse(rvaStr, System.Globalization.NumberStyles.HexNumber, null, out uint parsedRva))
                {
                    rva = parsedRva;
                }
                break;
            }
        }

        uint size = 16;  // Small sample size

        // Act
        string result = DnlibTools.ReadMemoryData(rva, size);
        string invalidResult = DnlibTools.ReadMemoryData(0xFFFFFFFF, size); // Very high RVA, likely invalid

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("No assembly loaded.", result);
        Assert.NotNull(invalidResult);
    }
    
    [Fact]
    public void UpdateMethodTest()
    {
        //Load a module (e.g., the current assembly)
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        // Example input string
        var input = "br.s IL_0020";
    
        try
        {
            string s;
            // Main, normal case
            s = DnlibTools.UpdateMethodInstructions(1,0,"nop\nret");
            Assert.Contains("successfully", s);
            // cctor, ins < body ins and offset + ins > body ins
            s = DnlibTools.UpdateMethodInstructions(7,5,"nop\nnop\nret");
            Assert.Contains("successfully", s);
            // cctor, ins > body ins
            s = DnlibTools.UpdateMethodInstructions(7,0,"nop\nnop\nnop\nret");
            Assert.Contains("successfully", s);

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    
    [Fact]
    public void UpdateMethodTest_2()
    {
        //Load a module (e.g., the current assembly)
        DnlibTools.LoadAssembly(_testAssemblyPath);
        
        var input = @"
            ldc.i4 2055
            ldc.i4.1
            ldc.i4.1
            newobj instance void [System.Runtime]System.DateTime::.ctor(int32, int32, int32)
            ret
        ";

        try
        {
            var s = DnlibTools.UpdateMethodInstructions(7,5,input);
            Assert.Contains("successfully", s);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}