using System;
using System.IO;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Crystal;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Crystal;

public sealed class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodePropertyWriter codePropertyWriter;
    private readonly CodeClass parentClass;
    private readonly CodeProperty property;

    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Crystal, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        var conventionService = new CrystalConventionService();
        codePropertyWriter = new CodePropertyWriter(conventionService);
        parentClass = new CodeClass
        {
            Name = "TestClass",
            Kind = CodeClassKind.Model
        };
        property = new CodeProperty
        {
            Name = "TestProperty",
            Type = new CodeType
            {
                Name = "String"
            },
            Parent = parentClass
        };
        parentClass.AddProperty(property);
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WritesProperty()
    {
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("property test_property : String", result);
    }

    [Fact]
    public void WritesReadOnlyProperty()
    {
        property.ReadOnly = true;
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("property test_property : String", result);
        Assert.Contains("getter", result);
        Assert.Contains("setter", result);
    }

    [Fact]
    public void WritesDeprecation()
    {
        property.Deprecation = new DeprecationInformation("This property is deprecated");
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("# This property is deprecated", result);
    }

    [Fact]
    public void WritesAdditionalDataProperty()
    {
        property.Kind = CodePropertyKind.AdditionalData;
        parentClass.AddBackingStoreProperty();
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("property test_property : String", result);
        Assert.Contains("getter", result);
        Assert.Contains("setter", result);
    }

    [Fact]
    public void WritesQueryParameterProperty()
    {
        property.Kind = CodePropertyKind.QueryParameter;
        property.SerializationName = "test_query";
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("@[QueryParameter(\"test_query\")]", result);
        Assert.Contains("property test_property : String", result);
    }

    [Fact]
    public void WritesDefaultValue()
    {
        property.DefaultValue = "\"default_value\"";
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("property test_property : String = \"default_value\"", result);
    }

    [Fact]
    public void WritesNullableProperty()
    {
        property.Type.IsNullable = true;
        codePropertyWriter.WriteCodeElement(property, writer);
        var result = tw.ToString();
        Assert.Contains("property test_property : String?", result);
    }
}


