using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
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
        Assert.Throws<InvalidOperationException>(() => writer.Write(property));
    }
    [Fact]
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"{PropertyName}?: {TypeName}", result);
        Assert.DoesNotContain("| undefined", result); // redundant with ?
    }
    [Fact]
    public void WritesFlagEnums()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Type = new CodeType
        {
            Name = "customEnum",
        };
        (property.Type as CodeType).TypeDefinition = new CodeEnum
        {
            Name = "customEnumType",
            Flags = true,
        };
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("[]", result);
    }
    [Fact]
    public void WritesCollectionFlagEnumsAsOneDimensionalArray()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Type = new CodeType
        {
            Name = "customEnum",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array
        };
        (property.Type as CodeType).TypeDefinition = new CodeEnum
        {
            Name = "customEnumType",
            Flags = true,//this is not expected for a collection. So treat as enum collection
        };
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("[]", result);
        Assert.DoesNotContain("[] []", result);
    }
}
