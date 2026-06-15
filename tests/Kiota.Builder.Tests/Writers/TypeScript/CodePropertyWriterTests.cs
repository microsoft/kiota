using System;
using System.IO;
using System.Linq;
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
    private readonly CodeInterface parentInterface;
    private const string PropertyName = "propertyName";
    private const string TypeName = "Somecustomtype";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var root = CodeNamespace.InitRootNamespace();
        parentInterface = root.AddInterface(new CodeInterface
        {
            Name = "parentClass",
            OriginalClass = new CodeClass() { Name = "parentClass" }
        }).First();
        property = new CodeProperty
        {
            Name = PropertyName,
            Type = new CodeType
            {
                Name = TypeName
            }
        };
        parentInterface.AddProperty(property, new()
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
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("?", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void WritesCustomPropertyWithDefaultedNullType()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"{PropertyName}?: {TypeName} | null", result);
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
    [Fact]
    public void FailsOnPropertiesForClasses()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Parent = new CodeClass
        {
            Name = "parentClass"
        };
        Assert.Throws<InvalidOperationException>(() => writer.Write(property));
    }
    [Fact]
    public void WritesCorrectTypeForProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Type = new CodeType
        {
            Name = "base64"
        };
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"{PropertyName}?: ArrayBuffer | null", result);
    }
    [Fact]
    public void DoesNodeEmitAdditionalDataPropertyOnInterfaces()
    {

        property.Kind = CodePropertyKind.AdditionalData;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Empty(result);
    }

    private (LanguageWriter, StringWriter) GetFlagOnWriter()
    {
        var flagOnWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName, makeRequiredPropertiesNonNullable: true);
        var sw = new StringWriter();
        flagOnWriter.SetTextWriter(sw);
        return (flagOnWriter, sw);
    }

    [Fact]
    public void WritesRequiredNonNullableProperty_FlagOn_NoOptionalNoNull()
    {
        var (flagOnWriter, sw) = GetFlagOnWriter();
        property.Kind = CodePropertyKind.Custom;
        property.Type.IsNullable = false;
        property.IsRequired = true;
        flagOnWriter.Write(property);
        var result = sw.ToString();
        Assert.Contains($"{PropertyName}: {TypeName};", result);
        Assert.DoesNotContain($"{PropertyName}?:", result);
        Assert.DoesNotContain("| null", result);
    }

    [Fact]
    public void WritesRequiredNullableProperty_FlagOn_NoOptionalKeepsNull()
    {
        var (flagOnWriter, sw) = GetFlagOnWriter();
        property.Kind = CodePropertyKind.Custom;
        property.Type.IsNullable = true;
        property.IsRequired = true;
        flagOnWriter.Write(property);
        var result = sw.ToString();
        Assert.Contains($"{PropertyName}: {TypeName} | null;", result);
        Assert.DoesNotContain($"{PropertyName}?:", result);
    }

    [Fact]
    public void WritesOptionalProperty_FlagOn_OptionalAndNull()
    {
        var (flagOnWriter, sw) = GetFlagOnWriter();
        property.Kind = CodePropertyKind.Custom;
        property.Type.IsNullable = false;
        property.IsRequired = false;
        flagOnWriter.Write(property);
        var result = sw.ToString();
        Assert.Contains($"{PropertyName}?: {TypeName} | null;", result);
    }

    [Fact]
    public void WritesRequiredProperty_FlagOff_OptionalAndNull()
    {
        // Opt-out: with the flag off, a required non-nullable property keeps the historical optional+nullable form.
        property.Kind = CodePropertyKind.Custom;
        property.Type.IsNullable = false;
        property.IsRequired = true;
        writer.Write(property); // default writer has the flag off
        var result = tw.ToString();
        Assert.Contains($"{PropertyName}?: {TypeName} | null;", result);
    }
}
