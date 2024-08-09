using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public class TypeScriptConventionServiceTests
{
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
