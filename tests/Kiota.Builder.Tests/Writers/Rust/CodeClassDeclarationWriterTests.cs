using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Rust;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeNamespace root;

    public CodeClassDeclarationWriterTests()
    {
        codeElementWriter = new CodeClassDeclarationWriter(new RustConventionService());
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Rust, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesModelStruct()
    {
        var modelClass = new CodeClass
        {
            Name = "TestModel",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        modelClass.AddProperty(new CodeProperty
        {
            Name = "displayName",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        });
        modelClass.AddProperty(new CodeProperty
        {
            Name = "age",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "integer" },
        });
        codeElementWriter.WriteCodeElement(modelClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("pub struct TestModel {", result);
        Assert.Contains("#[derive(Debug, Clone, Default, PartialEq)]", result);
        Assert.Contains("pub display_name:", result);
        Assert.Contains("pub age:", result);
        // Parsable impl
        Assert.Contains("impl Parsable for TestModel {", result);
        Assert.Contains("fn field_names(&self) -> Vec<&'static str>", result);
        Assert.Contains("fn assign_field(&mut self, field: &str, node: &dyn ParseNode)", result);
        Assert.Contains("fn serialize(&self, writer: &mut dyn SerializationWriter)", result);
    }
    [Fact]
    public void WritesRequestBuilderStruct()
    {
        var rbClass = new CodeClass
        {
            Name = "UsersRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        };
        root.AddClass(rbClass);
        rbClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseRequestBuilder",
            IsExternal = true,
        };
        rbClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType { Name = "string" },
        });
        rbClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType { Name = "RequestAdapter" },
        });
        rbClass.AddProperty(new CodeProperty
        {
            Name = "UrlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType { Name = "string" },
        });
        codeElementWriter.WriteCodeElement(rbClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("pub struct UsersRequestBuilder {", result);
        Assert.Contains("pub base: BaseRequestBuilder,", result);
        // base properties should not be duplicated as separate fields
        Assert.DoesNotContain("pub path_parameters:", result);
        Assert.DoesNotContain("pub request_adapter:", result);
    }
    [Fact]
    public void WritesImports()
    {
        var modelClass = new CodeClass
        {
            Name = "Invoice",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        modelClass.AddProperty(new CodeProperty
        {
            Name = "amount",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        });
        codeElementWriter.WriteCodeElement(modelClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("use kiota_abstractions::", result);
        Assert.Contains("Parsable", result);
        Assert.Contains("ParseNode", result);
        Assert.Contains("SerializationWriter", result);
    }
    [Fact]
    public void WritesGeneratedCodeComment()
    {
        var modelClass = new CodeClass
        {
            Name = "Marker",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        modelClass.AddProperty(new CodeProperty
        {
            Name = "id",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        });
        codeElementWriter.WriteCodeElement(modelClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("DO NOT EDIT", result);
    }
}
