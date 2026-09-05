using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class GoConventionServiceTests
{
    private readonly GoConventionService instance = new();
    [Fact]
    public void ThrowsOnInvalidOverloads()
    {
        var root = CodeNamespace.InitRootNamespace();
        Assert.Throws<InvalidOperationException>(() => instance.GetAccessModifier(AccessModifier.Private));
    }
    [Fact]
    public void SanitizesLineBreaksInDocumentationComments()
    {
        var codeClass = new CodeClass
        {
            Name = "testClass",
            Documentation = new()
            {
                DescriptionTemplate = "line1\r\nline2\tline3",
            },
        };
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, "./", "name");
        using var textWriter = new StringWriter();
        writer.SetTextWriter(textWriter);

        instance.WriteShortDescription(codeClass, writer);
        var result = textWriter.ToString();

        Assert.Contains("// line1line2 line3", result);
        Assert.DoesNotContain($"{GoTestConstants.LineFeed}line2", result);
    }
    [Fact]
    public void DoesNotStarNullableTypeParameterProperties()
    {
        // type parameters are constrained to Parsable (already a reference type), so a pointer
        // would be legal Go but wrong at the usage sites
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass { Name = "parentClass" }).First();
        var typeParameter = new CodeTypeParameter { Name = "TItemType" };
        parentClass.StartBlock.AddTypeParameter(typeParameter);
        var nullableParameterType = new CodeType
        {
            Name = "TItemType",
            TypeDefinition = typeParameter,
            IsNullable = true,
        };
        Assert.Equal("TItemType", instance.GetTypeString(nullableParameterType, parentClass));
        var nullableCollectionType = new CodeType
        {
            Name = "TItemType",
            TypeDefinition = typeParameter,
            IsNullable = true,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        Assert.Equal("[]TItemType", instance.GetTypeString(nullableCollectionType, parentClass));
    }
    [Fact]
    public void RendersGenericTypeArgumentsAndDeclaration()
    {
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass { Name = "parentClass" }).First();
        var typeParameter = new CodeTypeParameter { Name = "TItemType" };
        parentClass.StartBlock.AddTypeParameter(typeParameter);
        Assert.Equal($"[TItemType {GoTestConstants.SerializationHashPrefix}Parsable]", instance.GetTypeParametersDeclaration(parentClass.TypeParameters));
        Assert.Equal(string.Empty, instance.GetTypeParametersDeclaration([]));
        var closedType = new CodeType
        {
            Name = "paginatedTemplateable",
            TypeDefinition = root.AddInterface(new CodeInterface { Name = "PaginatedTemplateable", OriginalClass = parentClass }).First(),
        };
        closedType.AddGenericTypeParameterValue(new CodeType { Name = "TItemType", TypeDefinition = typeParameter });
        Assert.Equal("PaginatedTemplateable[TItemType]", instance.GetTypeString(closedType, parentClass, false, false));
    }
}
