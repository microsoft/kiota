using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Java;
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
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
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
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"return new {TypeName}", result);
        Assert.Contains("requestAdapter", result);
        Assert.Contains("pathParameters", result);
    }
    [Fact]
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"{TypeName} {PropertyName}", result);
        Assert.Contains("@jakarta.annotation.Nullable", result);
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
        Assert.Contains("EnumSet", result);
    }
    [Fact]
    public void WritesNonNull()
    {
        property.Kind = CodePropertyKind.Custom;
        (property.Type as CodeType).IsNullable = false;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("@jakarta.annotation.Nonnull", result);
    }
    [Fact]
    public void WritesCollectionFlagEnumsAsOneDimensionalArray()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Type = new CodeType
        {
            Name = "customEnum",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex
        };
        (property.Type as CodeType).TypeDefinition = new CodeEnum
        {
            Name = "customEnumType",
            Flags = true,//this is not expected for a collection. So treat as enum collection
        };
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("List<", result);
        Assert.DoesNotContain("EnumSet", result);
    }
    [Fact]
    public void WritesQueryParameterWithDefaultEnumValue()
    {
        var enumProperty = new CodeProperty
        {
            Name = "EnumProperty",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum
                {
                    Name = "SomeEnum"
                }
            },
            Kind = CodePropertyKind.QueryParameter,
            DefaultValue = "EnumValue2",
        };
        parentClass.AddProperty(enumProperty, new()
        {
            Name = "queryParameter",
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "QueryParameter",
            },
        });
        writer.Write(enumProperty);
        var result = tw.ToString();
        Assert.Contains("SomeEnum EnumProperty = SomeEnum.EnumValue2;", result);
    }
    [Fact]
    public void WritesQueryParameterWithDefaultIntValue()
    {
        var integerProperty = new CodeProperty
        {
            Name = "IntegerProperty",
            Type = new CodeType
            {
                Name = "Integer"
            },
            Kind = CodePropertyKind.QueryParameter,
            DefaultValue = "3",
        };
        parentClass.AddProperty(integerProperty, new()
        {
            Name = "queryParameter",
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "QueryParameter",
            },
        });
        writer.Write(integerProperty);
        var result = tw.ToString();
        Assert.Contains("Integer IntegerProperty = 3;", result);
    }
    [Fact]
    public void WritesQueryParameterWithDefaultStringValue()
    {
        var stringProperty = new CodeProperty
        {
            Name = "StringProperty",
            Type = new CodeType
            {
                Name = "String"
            },
            Kind = CodePropertyKind.QueryParameter,
            DefaultValue = "\"SomeString\"",
        };
        parentClass.AddProperty(stringProperty, new()
        {
            Name = "queryParameter",
            Kind = CodePropertyKind.QueryParameter,
            Type = new CodeType
            {
                Name = "QueryParameter",
            },
        });
        writer.Write(stringProperty);
        var result = tw.ToString();
        Assert.Contains("String StringProperty = \"SomeString\";", result);
    }
}
