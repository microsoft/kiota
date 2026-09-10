using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public sealed class CodePropertyWriterTests : IDisposable
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Rust, DefaultPath, DefaultName);
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
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("pub", result);
        Assert.Contains("property_name", result);
    }
    [Fact]
    public void WritesSerdeRenameAttribute()
    {
        property.Kind = CodePropertyKind.Custom;
        property.SerializationName = "PropertyName";
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("#[serde(rename = \"PropertyName\")]", result);
    }
    [Fact]
    public void WritesRequestBuilderAsEmpty()
    {
        // In Rust, request builder properties are accessed via methods, not struct fields
        property.Kind = CodePropertyKind.RequestBuilder;
        writer.Write(property);
        var result = tw.ToString();
        // No field is written for request builder properties
        Assert.DoesNotContain("pub", result);
    }
    [Fact]
    public void DoesntWritePropertiesExistingInParentType()
    {
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
        var subClass = (parentClass.Parent as CodeNamespace)!.AddClass(new CodeClass
        {
            Name = "BaseClass",
        }).First();
        subClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseClass",
            TypeDefinition = parentClass
        };
        var propertyToWrite = subClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        }).First();
        writer.Write(propertyToWrite);
        var result = tw.ToString();
        Assert.Empty(result);
    }
}
