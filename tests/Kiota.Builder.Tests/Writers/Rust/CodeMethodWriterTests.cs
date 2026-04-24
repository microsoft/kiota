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
    private CodeMethod method;
    private CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Rust, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    private void setup(bool withInheritance = false)
    {
        if (parentClass != null)
            throw new InvalidOperationException("setup() must only be called once");
        CodeClass baseClass = default;
        if (withInheritance)
        {
            baseClass = root.AddClass(new CodeClass
            {
                Name = "SomeParentClass",
            }).First();
            baseClass.AddProperty(new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "String"
                },
                Kind = CodePropertyKind.Custom,
            });
        }
        parentClass = new CodeClass
        {
            Name = "ParentClass"
        };
        if (withInheritance)
        {
            parentClass.StartBlock.Inherits = new CodeType
            {
                Name = "SomeParentClass",
                TypeDefinition = baseClass
            };
        }
        root.AddClass(parentClass);
        method = new CodeMethod
        {
            Name = MethodName,
            ReturnType = new CodeType
            {
                Name = ReturnTypeName
            }
        };
        parentClass.AddMethod(method);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    private void AddRequestProperties()
    {
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseRequestBuilder",
            IsExternal = true,
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "String",
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "String",
            }
        });
    }
    private void AddSerializationProperties()
    {
        parentClass.AddProperty(new CodeProperty
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = "String"
            },
            Getter = new CodeMethod
            {
                Name = "getAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "String"
                }
            },
            Setter = new CodeMethod
            {
                Name = "setAdditionalData",
                ReturnType = new CodeType
                {
                    Name = "String"
                }
            }
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
            Type = new CodeType
            {
                Name = "String"
            },
            Kind = CodePropertyKind.Custom,
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyUCaseProp",
            Type = new CodeType
            {
                Name = "String"
            },
            Kind = CodePropertyKind.Custom,
            SerializationName = "DummyUCaseProp",
        });
    }
    [Fact]
    public void WritesSerializerBody()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        method.Kind = CodeMethodKind.Serializer;
        method.Name = "serialize";
        method.IsAsync = false;
        AddSerializationProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "writer",
            Kind = CodeParameterKind.Serializer,
            Type = new CodeType
            {
                Name = "SerializationWriter"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn serialize", result);
        Assert.Contains("write_string_value", result);
        Assert.Contains("Ok(())", result);
    }
    [Fact]
    public void WritesDeserializerBody()
    {
        setup();
        parentClass.Kind = CodeClassKind.Model;
        method.Kind = CodeMethodKind.Deserializer;
        method.Name = "get_field_deserializers";
        method.IsAsync = false;
        method.ReturnType = new CodeType
        {
            Name = "HashMap<String, Box<dyn Fn(&dyn ParseNode, &mut Self)>>"
        };
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn get_field_deserializers", result);
        Assert.Contains("HashMap", result);
    }
    [Fact]
    public void WritesConstructorBody()
    {
        setup();
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn new", result);
        Assert.Contains("Self", result);
    }
    [Fact]
    public void WritesMethodDescription()
    {
        setup();
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("///", result);
        Assert.Contains(MethodDescription, result);
    }
    [Fact]
    public void WritesAsyncMethod()
    {
        setup();
        method.Kind = CodeMethodKind.RequestExecutor;
        method.IsAsync = true;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "requestConfiguration",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "RequestConfiguration"
            },
            Optional = true,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub async fn", result);
        Assert.Contains("Result<", result);
    }
    [Fact]
    public void WritesRequestGeneratorBody()
    {
        setup();
        method.Kind = CodeMethodKind.RequestGenerator;
        method.IsAsync = false;
        method.HttpMethod = HttpMethod.Get;
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "requestConfiguration",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "RequestConfiguration"
            },
            Optional = true,
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("RequestInformation", result);
        Assert.Contains("url_template", result);
    }
    [Fact]
    public void WritesIndexerBackwardCompatibility()
    {
        setup();
        method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
        method.IsAsync = false;
        AddRequestProperties();
        method.AddParameter(new CodeParameter
        {
            Name = "id",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            },
            SerializationName = "id",
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("pub fn", result);
    }
}
