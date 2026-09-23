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
    /// <summary>
    /// Builds a code file holding a model interface and a constant, with a single using on the
    /// interface. A constant is not a block of its own, so the enclosing block of the returned
    /// target element is the code file, which is the case where the aliases of every child of the
    /// file have to be aggregated.
    /// </summary>
    private static (CodeConstant TargetElement, CodeType TargetType) BuildCodeFileWithUsing(string alias, bool isExternal = false, bool aliasAnotherType = false)
    {
        var root = CodeNamespace.InitRootNamespace();
        var modelsNS = root.AddNamespace("models");
        var modelInterface = new CodeInterface
        {
            Name = "Policy",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass { Name = "Policy" },
        };
        var otherInterface = new CodeInterface
        {
            Name = "OtherPolicy",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass { Name = "OtherPolicy" },
        };
        var declaredType = aliasAnotherType ? otherInterface : modelInterface;
        modelInterface.AddUsing(new CodeUsing
        {
            Name = declaredType.Name,
            Alias = alias,
            Declaration = new CodeType { Name = declaredType.Name, TypeDefinition = declaredType, IsExternal = isExternal },
        });
        var constant = new CodeConstant
        {
            Name = "policyMapper",
            Kind = CodeConstantKind.QueryParametersMapper,
        };
        modelsNS.TryAddCodeFile("policyFile", modelInterface, constant);
        return (constant, new CodeType { Name = "Policy", TypeDefinition = modelInterface });
    }

    [Fact]
    public void GetTypescriptTypeString_ReturnsAlias_WhenTheEnclosingCodeFileHasAnAliasedUsing()
    {
        var (targetElement, targetType) = BuildCodeFileWithUsing("SomeAliasedPolicy");

        var result = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);

        Assert.Equal("SomeAliasedPolicy", result);
    }

    [Fact]
    public void GetTypescriptTypeString_ReturnsTheSameAlias_WhenCalledRepeatedlyForTheSameBlock()
    {
        var (targetElement, targetType) = BuildCodeFileWithUsing("SomeAliasedPolicy");

        var first = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);
        var second = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);
        var third = TypeScriptConventionService.GetTypescriptTypeString(new CodeType { Name = targetType.Name, TypeDefinition = targetType.TypeDefinition }, targetElement, false);

        Assert.Equal("SomeAliasedPolicy", first);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void GetTypescriptTypeString_PicksUpAnAliasAssignedAfterAnEarlierLookup()
    {
        // AliasCollidingSymbols assigns Alias in place on a using that already exists, so a lookup that
        // ran before the assignment must not make the writers miss the alias afterwards.
        var (targetElement, targetType) = BuildCodeFileWithUsing(string.Empty);
        var codeUsing = ((CodeFile)targetElement.Parent!).GetChildElements(true)
                            .OfType<CodeInterface>()
                            .SelectMany(static x => x.Usings)
                            .Single();

        var before = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);
        codeUsing.Alias = "SomeAliasedPolicy";
        var after = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);

        Assert.Equal("Policy", before);
        Assert.Equal("SomeAliasedPolicy", after);
    }

    [Fact]
    public void GetTypescriptTypeString_IgnoresTheAlias_WhenTheUsingIsExternal()
    {
        var (targetElement, targetType) = BuildCodeFileWithUsing("SomeAliasedPolicy", isExternal: true);

        var result = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);

        Assert.Equal("Policy", result);
    }

    [Fact]
    public void GetTypescriptTypeString_IgnoresTheUsing_WhenItCarriesNoAlias()
    {
        var (targetElement, targetType) = BuildCodeFileWithUsing(string.Empty);

        var result = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);

        Assert.Equal("Policy", result);
    }

    [Fact]
    public void GetTypescriptTypeString_IgnoresTheAlias_WhenItBelongsToAnotherType()
    {
        var (targetElement, targetType) = BuildCodeFileWithUsing("SomeAliasedPolicy", aliasAnotherType: true);

        var result = TypeScriptConventionService.GetTypescriptTypeString(targetType, targetElement, false);

        Assert.Equal("Policy", result);
    }

    [Fact]
    public void RemoveInvalidDescriptionCharacters_SanitizesCommentBreakoutCharacters()
    {
        var result = TypeScriptConventionService.RemoveInvalidDescriptionCharacters("line1*/\r\nline2");
        Assert.Equal("line1* /line2", result);
    }
}
