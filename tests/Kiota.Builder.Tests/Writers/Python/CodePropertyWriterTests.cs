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
    private const string PropertyName = "propertyName";
    private const string TypeName = "Somecustomtype";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        property = new CodeProperty
        {
            Name = PropertyName,
            Type = new CodeType
            {
                Name = TypeName
            }
        };
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
        Assert.Contains("def property_name(", result);
        Assert.Contains("This is a request builder", result);
        Assert.Contains($"return {TypeName.ToLower()}.{TypeName}(", result);
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
        Assert.Contains($"property_name: Optional[{TypeName.ToLower()}.{TypeName}]", result);
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
