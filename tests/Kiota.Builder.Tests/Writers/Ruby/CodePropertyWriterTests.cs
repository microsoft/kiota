using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Ruby;

public sealed class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeProperty property;
    private readonly CodeClass parentClass;
    private readonly CodeClass EmptyClass;
    private readonly CodeProperty emptyProperty;
    private const string PropertyName = "propertyName";
    private const string TypeName = "SomeCustomClass";
    private const string RootNamespaceName = "RootNamespace";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        CodeNamespace.InitRootNamespace();
        EmptyClass = new CodeClass
        {
            Name = "emptyClass"
        };
        emptyProperty = new CodeProperty
        {
            Name = PropertyName,
            Type = new CodeType
            {
                Name = TypeName
            }
        };
        EmptyClass.AddProperty(emptyProperty, new()
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

        var root = CodeNamespace.InitRootNamespace();
        root.Name = RootNamespaceName;
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
                Name = TypeName,
                TypeDefinition = parentClass
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
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"def {PropertyName.ToSnakeCase()}", result);
        Assert.Contains($"{RootNamespaceName}::ParentClass.new", result);
        Assert.Contains("request_adapter", result);
        Assert.Contains("path_parameters", result);
    }
    [Fact]
    public void WritesRequestBuilderWithoutNamespace()
    {
        emptyProperty.Kind = CodePropertyKind.RequestBuilder;
        writer.Write(emptyProperty);
        var result = tw.ToString();
        Assert.Contains($"def {PropertyName.ToSnakeCase()}", result);
        Assert.Contains($"{TypeName}.new", result);
        Assert.Contains("request_adapter", result);
        Assert.Contains("path_parameters", result);
        Assert.DoesNotContain($"::{TypeName}.new", result);
    }
    [Fact]
    public void WritesAccessedProperty()
    {
        emptyProperty.Kind = CodePropertyKind.QueryParameter;
        writer.Write(emptyProperty);
        var result = tw.ToString();
        Assert.Contains($"attr_accessor :{PropertyName.ToSnakeCase()}", result);
    }
    [Fact]
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"@{PropertyName.ToSnakeCase()}", result);
    }
}
