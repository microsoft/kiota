using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.Http;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Http;

public sealed class HttpConventionServiceTest
{

    [Fact]
    public void TestGetDefaultValueForProperty_Int()
    {
        // Arrange
        var codeProperty = new CodeProperty
        {
            Type = new CodeType { Name = "int" }
        };

        // Act
        var result = HttpConventionService.GetDefaultValueForProperty(codeProperty);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void TestGetDefaultValueForProperty_String()
    {
        // Arrange
        var codeProperty = new CodeProperty
        {
            Type = new CodeType { Name = "string" }
        };

        // Act
        var result = HttpConventionService.GetDefaultValueForProperty(codeProperty);

        // Assert
        Assert.Equal("\"string\"", result);
    }

    [Fact]
    public void TestGetDefaultValueForProperty_Bool()
    {
        // Arrange
        var codeProperty = new CodeProperty
        {
            Type = new CodeType { Name = "bool" }
        };

        // Act
        var result = HttpConventionService.GetDefaultValueForProperty(codeProperty);

        // Assert
        Assert.Equal("false", result);
    }

    [Fact]
    public void TestGetDefaultValueForProperty_Null()
    {
        // Arrange
        var codeProperty = new CodeProperty
        {
            Type = new CodeType { Name = "unknown" }
        };

        // Act
        var result = HttpConventionService.GetDefaultValueForProperty(codeProperty);

        // Assert
        Assert.Equal("null", result);
    }
}
