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

    [Fact]
    public void GetFactoryMethodName_ReturnsCamelCase_WhenTypeIsAliasedButMethodIsNot()
    {
        // Arrange - create model interface and factory method in a code file
        var root = CodeNamespace.InitRootNamespace();
        var modelsNS = root.AddNamespace("models");

        var modelInterface = new CodeInterface { Name = "Policy", Kind = CodeInterfaceKind.Model, OriginalClass = new CodeClass { Name = "Policy" } };

        // CodeFunction requires a static method parented by a CodeClass
        var parentClass = modelsNS.AddClass(new CodeClass { Name = "Policy" }).First();
        var factoryMethod = parentClass.AddMethod(new CodeMethod
        {
            Name = "createPolicyFromDiscriminatorValue",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType { Name = "Policy", TypeDefinition = modelInterface },
            IsStatic = true,
        }).First();
        var codeFunction = new CodeFunction(factoryMethod);

        // Place interface and factory function in the same CodeFile
        modelsNS.TryAddCodeFile("policyFile", modelInterface, codeFunction);

        // Create a consumer element that has an aliased using for the type but NOT for the factory method
        var consumerNS = root.AddNamespace("consumer");
        var consumerClass = consumerNS.AddClass(new CodeClass { Name = "Consumer" }).First();
        consumerClass.AddUsing(new CodeUsing
        {
            Name = "Policy",
            Alias = "SomeAliasedPolicy",
            Declaration = new CodeType { Name = "Policy", TypeDefinition = modelInterface },
        });

        var targetType = new CodeType { Name = "Policy", TypeDefinition = modelInterface };

        // Act
        var result = TypeScriptConventionService.GetFactoryMethodName(targetType, consumerClass);

        // Assert - should be camelCase, not PascalCase
        Assert.Equal("createPolicyFromDiscriminatorValue", result);
    }
}
