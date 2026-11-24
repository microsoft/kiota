using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public sealed class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeProperty property;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace ns;
    private const string PropertyName = "property_name";
    private const string TypeName = "Somecustomtype";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        ns = root.AddNamespace("Graphtests.models");
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        ns.AddClass(parentClass);
        property = new CodeProperty
        {
            Name = PropertyName,
            Type = new CodeType
            {
                Name = TypeName
            }
        };
        var subNS = ns.AddNamespace($"{ns.Name}.somecustomtype");
        var somecustomtypeClassDef = new CodeClass
        {
            Name = "Somecustomtype",
        };
        subNS.AddClass(somecustomtypeClassDef);
        var nUsing = new CodeUsing
        {
            Name = somecustomtypeClassDef.Name,
            Declaration = new()
            {
                Name = somecustomtypeClassDef.Name,
                TypeDefinition = somecustomtypeClassDef,
            }
        };
        parentClass.StartBlock.AddUsings(nUsing);
        parentClass.AddProperty(property, new()
        {
            Name = "path_parameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "PathParameters",
            },
        }, new()
        {
            Name = "request_adapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
            },
        });
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesRequestBuilder()
    {
        property.Kind = CodePropertyKind.RequestBuilder;
        property.Documentation.DescriptionTemplate = "This is a request builder";
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("@property", result);
        Assert.Contains("def property_name(self) -> Somecustomtype:", result);
        Assert.Contains("This is a request builder", result);
        Assert.Contains("from .somecustomtype.somecustomtype import Somecustomtype", result);
        Assert.Contains($"return {TypeName}(", result);
        Assert.Contains("self.request_adapter", result);
        Assert.Contains("self.path_parameters", result);
    }
    [Fact]
    public void WritesQueryParameters()
    {
        property.Kind = CodePropertyKind.QueryParameters;
        writer.Write(property);
        var result = tw.ToString();
        Assert.DoesNotContain("@property", result);
        Assert.Contains($"property_name: Optional[Graphtests.models.{TypeName}]", result);
    }
    [Fact]
    public void WritesDefaultValuesForProperties()
    {
        property.Kind = CodePropertyKind.Headers;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("= None", result);
    }

    [Fact]
    public void WritesDefaultValuesForNonNullableArrayPropertiesWhenDefaultValueSet()
    {
        property.Kind = CodePropertyKind.QueryParameter;
        property.Type.IsNullable = false;
        property.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        property.DefaultValue = "field(default_factory=list)";
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("= field(default_factory=list)", result);
    }

    [Fact]
    public void WritePrimaryErrorMessagePropertyOption1()
    {
        property.Kind = CodePropertyKind.ErrorMessageOverride;
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("super().message", result);
    }
    [Fact]
    public void WritePrimaryErrorMessagePropertyOption2()
    {
        property.Kind = CodePropertyKind.ErrorMessageOverride;
        var cls = new CodeClass
        {
            Name = "MainError",
            Documentation = new CodeDocumentation { DescriptionTemplate = "Some documentation" }
        };
        cls.AddProperty(new CodeProperty { Name = "message", Type = new CodeType { Name = "str" }, IsPrimaryErrorMessage = true });
        property.Type.Name = "str";
        parentClass.AddProperty(new CodeProperty { Name = "error", Type = new CodeType { IsExternal = false, Name = "MainError", TypeDefinition = cls } });
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("if self.error is not None:", result);
        Assert.Contains("return '' if self.error.message is None else self.error.message", result);
    }
    [Theory]
    [InlineData("developer_error_message")]
    [InlineData("message")]
    public void WritePrimaryErrorMessagePropertyOfTheErrorItself(string primaryMessagePropertyName)
    {
        property.Kind = CodePropertyKind.ErrorMessageOverride;
        property.Type.Name = "str";
        parentClass.AddProperty(new CodeProperty { Name = primaryMessagePropertyName, Type = new CodeType { Name = "str" }, IsPrimaryErrorMessage = true });
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"if self.{primaryMessagePropertyName} is not None:", result);
        Assert.Contains($"return '' if self.{primaryMessagePropertyName} is None else self.{primaryMessagePropertyName}", result);
    }
    [Fact]
    public void WritePrimaryErrorMessagePropertyOfANestedObject()
    {
        property.Kind = CodePropertyKind.ErrorMessageOverride;
        var cls = new CodeClass
        {
            Name = "MainError",
            Documentation = new CodeDocumentation { DescriptionTemplate = "Some documentation" }
        };
        cls.AddProperty(new CodeProperty { Name = "message", Type = new CodeType { Name = "str" }, IsPrimaryErrorMessage = true });
        property.Type.Name = "str";
        parentClass.AddProperty(new CodeProperty { Name = "response", Type = new CodeType { IsExternal = false, Name = "MainError", TypeDefinition = cls } });
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("if self.response is not None:", result);
        Assert.Contains("return '' if self.response.message is None else self.response.message", result);
        Assert.DoesNotContain("self.respon ", result);
    }
    [Fact]
    public void WritePrimaryErrorMessagePropertyTwoLevelsDeep()
    {
        property.Kind = CodePropertyKind.ErrorMessageOverride;
        var inner = new CodeClass
        {
            Name = "InnerError",
            Documentation = new CodeDocumentation { DescriptionTemplate = "Some documentation" }
        };
        inner.AddProperty(new CodeProperty { Name = "message", Type = new CodeType { Name = "str" }, IsPrimaryErrorMessage = true });
        var outer = new CodeClass
        {
            Name = "OuterError",
            Documentation = new CodeDocumentation { DescriptionTemplate = "Some documentation" }
        };
        outer.AddProperty(new CodeProperty { Name = "inner", Type = new CodeType { IsExternal = false, Name = "InnerError", TypeDefinition = inner } });
        property.Type.Name = "str";
        parentClass.AddProperty(new CodeProperty { Name = "outer", Type = new CodeType { IsExternal = false, Name = "OuterError", TypeDefinition = outer } });
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("if self.outer is not None and self.outer.inner is not None:", result);
        Assert.Contains("return '' if self.outer.inner.message is None else self.outer.inner.message", result);
        Assert.DoesNotContain("if self.outer.inner is not None:", result);
    }
    [Theory]
    [InlineData("primaryMessage")]
    [InlineData("primary_message")]
    public async Task WritePrimaryErrorMessagePropertyNamedLikeTheOverrideAsync(string primaryMessagePropertyName)
    {
        var root = CodeNamespace.InitRootNamespace();
        var model = root.AddNamespace("models").AddClass(new CodeClass
        {
            Name = "CollidingError",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).Single();
        model.AddProperty(new CodeProperty
        {
            Name = primaryMessagePropertyName,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string", IsExternal = true },
            IsPrimaryErrorMessage = true,
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.Python }, root, cancellationToken: TestContext.Current.CancellationToken);
        writer.Write(model.Properties.Single(static x => x.Kind is CodePropertyKind.ErrorMessageOverride));
        var result = tw.ToString();
        Assert.Contains("def primary_message(self) -> Optional[str]:", result);
        Assert.Contains("if self.primary_messageEscaped is not None:", result);
        Assert.Contains("return '' if self.primary_messageEscaped is None else self.primary_messageEscaped", result);
        Assert.DoesNotContain("self.primary_message ", result);
    }
}
