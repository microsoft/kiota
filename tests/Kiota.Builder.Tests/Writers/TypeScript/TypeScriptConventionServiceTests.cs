using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public class TypeScriptConventionServiceTests
{
    [Fact]
    public void GetParentOfTypeOrNull_ShouldThrowArgumentNullException_WhenCodeElementIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => TypeScriptConventionService.GetParentOfTypeOrNull<CodeElement>(null));
    }

    [Fact]
    public void GetParentOfTypeOrNull_ShouldReturnNull_WhenNoParentOfTypeExists()
    {
        var codeElement = new CodeProperty { Name = "Test Property", Type = new CodeType { Name = "test" } };
        Assert.Null(TypeScriptConventionService.GetParentOfTypeOrNull<CodeClass>(codeElement));
    }

    [Fact]
    public void GetParentOfTypeOrNull_ShouldReturnParent_WhenParentOfTypeExists()
    {
        var parent = new CodeClass();
        var codeElement = new CodeProperty { Name = "Test Property", Parent = parent, Type = new CodeType { Name = "test" } };
        Assert.Equal(parent, TypeScriptConventionService.GetParentOfTypeOrNull<CodeClass>(codeElement));
    }

    [Fact]
    public void TranslateType_ThrowsArgumentNullException_WhenComposedTypeIsNull()
    {
        var result = TypeScriptConventionService.TranslateTypescriptType(null);
        Assert.Equal(TypeScriptConventionService.TYPE_OBJECT, result);
    }

    [Fact]
    public void TranslateType_ReturnsCorrectTranslation_WhenComposedTypeIsNotNull()
    {
        var composedType = new CodeUnionType { Name = "test" };
        var result = TypeScriptConventionService.TranslateTypescriptType(composedType);
        Assert.Equal("Test", result);
    }
}
