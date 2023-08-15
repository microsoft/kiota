using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;
public class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeProperty property;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace ns;
    private const string PropertyName = "propertyName";
    private const string TypeName = "Somecustomtype";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        ns = root.AddNamespace("graphtests.models");
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
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "PathParameters",
            },
        }, new()
        {
            Name = "requestAdapter",
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
}
