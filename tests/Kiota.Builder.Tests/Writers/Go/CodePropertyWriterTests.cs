using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
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
        parentClass.AddProperty(property);
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
    public void WritingThePropertyItselfEmitsNothing()
    {
        // fields are emitted by CodeClassDeclarationWriter so the whole struct body can be
        // column-aligned like gofmt; dispatching the property alone writes nothing
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        Assert.Empty(tw.ToString());
    }
    [Fact]
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(parentClass.StartBlock);
        var result = tw.ToString();
        Assert.Contains($"{PropertyName.ToFirstCharacterUpperCase()} *{TypeName}", result);
    }
    [Fact(Skip = "flag enum support needs to be added in Go")]
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
        writer.Write(parentClass.StartBlock);
        var result = tw.ToString();
        Assert.Contains("EnumSet", result);
    }
    [Fact]
    public void WritesNonNull()
    {
        property.Kind = CodePropertyKind.Custom;
        (property.Type as CodeType).IsNullable = false;
        writer.Write(parentClass.StartBlock);
        var result = tw.ToString();
        Assert.Contains($"{PropertyName.ToFirstCharacterUpperCase()} {TypeName}", result);
        Assert.DoesNotContain($"*{TypeName}", result);
    }
    [Fact]
    public void WritesSerializationTag()
    {
        property.Kind = CodePropertyKind.QueryParameter;
        property.SerializationName = "someserializationname";
        writer.Write(parentClass.StartBlock);
        var result = tw.ToString();
        Assert.Contains("\"uriparametername:\\\"someserializationname\\\"\"", result);
    }
    [Fact]
    public void EscapesSerializationTag()
    {
        property.Kind = CodePropertyKind.QueryParameter;
        property.SerializationName = "line1\"\nline2";
        writer.Write(parentClass.StartBlock);
        var result = tw.ToString();
        Assert.Contains("uriparametername:\\\"line1", result);
        Assert.Contains("line2\\\"", result);
        Assert.Contains("\\n", result);
    }
}
