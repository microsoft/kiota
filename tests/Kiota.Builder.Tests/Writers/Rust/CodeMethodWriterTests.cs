using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Rust;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public sealed class CodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeNamespace root;

    public CodeMethodWriterTests()
    {
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
    public void WritesConstructor()
    {
        var modelClass = new CodeClass
        {
            Name = "Payment",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        var method = new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType { Name = "void" },
        };
        modelClass.AddMethod(method);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn new() -> Self {", result);
        Assert.Contains("Self::default()", result);
    }
    [Fact]
    public void WritesRequestBuilderConstructor()
    {
        var rbClass = new CodeClass
        {
            Name = "PaymentsRequestBuilder",
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
            DefaultValue = "\"{+baseurl}/payments\"",
        });
        var method = new CodeMethod
        {
            Name = "constructor",
            Kind = CodeMethodKind.Constructor,
            ReturnType = new CodeType { Name = "void" },
        };
        method.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType { Name = "string" },
        });
        method.AddParameter(new CodeParameter
        {
            Name = "requestAdapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = new CodeType { Name = "RequestAdapter" },
        });
        rbClass.AddMethod(method);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn new(path_parameters: std::collections::HashMap<String, String>, request_adapter: std::sync::Arc<dyn RequestAdapter>) -> Self {", result);
        Assert.Contains("base: BaseRequestBuilder::new(request_adapter,", result);
        Assert.Contains("{+baseurl}/payments", result);
    }
    [Fact]
    public void WritesGetter()
    {
        var modelClass = new CodeClass
        {
            Name = "Invoice",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        var prop = new CodeProperty
        {
            Name = "amount",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        };
        modelClass.AddProperty(prop);
        var getter = new CodeMethod
        {
            Name = "GetAmount",
            Kind = CodeMethodKind.Getter,
            ReturnType = new CodeType { Name = "string" },
            AccessedProperty = prop,
        };
        modelClass.AddMethod(getter);
        writer.Write(getter);
        var result = tw.ToString();
        Assert.Contains("pub fn get_amount(&self) ->", result);
        Assert.Contains("&self.amount", result);
    }
    [Fact]
    public void WritesSetter()
    {
        var modelClass = new CodeClass
        {
            Name = "Invoice",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        var prop = new CodeProperty
        {
            Name = "amount",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { Name = "string" },
        };
        modelClass.AddProperty(prop);
        var setter = new CodeMethod
        {
            Name = "SetAmount",
            Kind = CodeMethodKind.Setter,
            ReturnType = new CodeType { Name = "void" },
            AccessedProperty = prop,
        };
        modelClass.AddMethod(setter);
        writer.Write(setter);
        var result = tw.ToString();
        Assert.Contains("pub fn set_amount(&mut self, value:", result);
        Assert.Contains("self.amount = value;", result);
    }
    [Fact]
    public void WritesFactoryMethod()
    {
        var modelClass = new CodeClass
        {
            Name = "Payment",
            Kind = CodeClassKind.Model,
        };
        root.AddClass(modelClass);
        var factory = new CodeMethod
        {
            Name = "createFromDiscriminatorValue",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType { Name = "Payment" },
            IsStatic = true,
        };
        modelClass.AddMethod(factory);
        writer.Write(factory);
        var result = tw.ToString();
        Assert.Contains("pub fn create_from_discriminator_value(_parse_node: &dyn ParseNode) -> Result<Self, KiotaError> {", result);
        Assert.Contains("Ok(Self::default())", result);
    }
}
