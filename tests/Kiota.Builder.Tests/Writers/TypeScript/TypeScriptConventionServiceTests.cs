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

    public CodeType CurrentType()
    {
        CodeType currentType = new CodeType { Name = "SomeType" };
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass { Name = "ParentClass" }).First();
        currentType.Parent = parentClass;
        return currentType;
    }

    [Fact]
    public void IsComposedOfPrimitives_ShouldBeTrue_WhenComposedOfPrimitives()
    {
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };
        composedType.AddType(new CodeType { Name = "string", IsExternal = true });
        composedType.AddType(new CodeType { Name = "integer", IsExternal = true });
        Assert.True(composedType.IsComposedOfPrimitives(TypeScriptConventionService.IsPrimitiveType));
    }

    [Fact]
    public void IsComposedOfPrimitives_ShouldBeFalse_WhenNotComposedOfPrimitives()
    {
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };
        composedType.AddType(new CodeType { Name = "string", IsExternal = true });
        var td = new CodeClass { Name = "SomeClass" };
        composedType.AddType(new CodeType { Name = "SomeCustomObject", IsExternal = false, TypeDefinition = td });
        Assert.False(composedType.IsComposedOfPrimitives(TypeScriptConventionService.IsPrimitiveType));
    }

    [Fact]
    public void IsComposedOfObjectsAndPrimitives_OnlyPrimitives_ReturnsFalse()
    {
        // Arrange
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };

        composedType.AddType(new CodeType { Name = "string", IsExternal = true });

        // Act
        var result = composedType.IsComposedOfObjectsAndPrimitives(TypeScriptConventionService.IsPrimitiveType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComposedOfObjectsAndPrimitives_OnlyObjects_ReturnsFalse()
    {
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };

        var td = new CodeClass { Name = "SomeClass" };
        composedType.AddType(new CodeType { Name = "SomeCustomObject", IsExternal = false, TypeDefinition = td });

        // Act
        var result = composedType.IsComposedOfObjectsAndPrimitives(TypeScriptConventionService.IsPrimitiveType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComposedOfObjectsAndPrimitives_BothPrimitivesAndObjects_ReturnsTrue()
    {
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };
        // Add primitive
        composedType.AddType(new CodeType { Name = "string", IsExternal = true });
        var td = new CodeClass { Name = "SomeClass" };
        composedType.AddType(new CodeType { Name = "SomeCustomObject", IsExternal = false, TypeDefinition = td });

        // Act
        var result = composedType.IsComposedOfObjectsAndPrimitives(TypeScriptConventionService.IsPrimitiveType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsComposedOfObjectsAndPrimitives_EmptyTypes_ReturnsFalse()
    {
        // Arrange
        var composedType = new CodeUnionType { Name = "test", Parent = CurrentType() };

        // Act
        var result = composedType.IsComposedOfObjectsAndPrimitives(TypeScriptConventionService.IsPrimitiveType);

        // Assert
        Assert.False(result);
    }
}
