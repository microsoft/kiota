using System;
using System.IO;

using Kiota.Builder.CodeDOM;
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
        property.Documentation.Description = "This is a request builder";
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
            Documentation = new CodeDocumentation { Description = "Some documentation" }
        };
        cls.AddProperty(new CodeProperty { Name = "message", Type = new CodeType { Name = "str" }, IsPrimaryErrorMessage = true });
        property.Type.Name = "str";
        parentClass.AddProperty(new CodeProperty { Name = "error", Type = new CodeType { IsExternal = false, Name = "MainError", TypeDefinition = cls } });
        parentClass.IsErrorDefinition = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("return '' if self.error.message is None else self.error.message", result);
    }
}
