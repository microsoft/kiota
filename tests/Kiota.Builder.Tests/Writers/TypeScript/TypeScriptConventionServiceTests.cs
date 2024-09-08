using System.Linq;
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

    private static CodeType CurrentType()
    {
        CodeType currentType = new CodeType { Name = "SomeType" };
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass { Name = "ParentClass" }).First();
        currentType.Parent = parentClass;
        return currentType;
    }
}
