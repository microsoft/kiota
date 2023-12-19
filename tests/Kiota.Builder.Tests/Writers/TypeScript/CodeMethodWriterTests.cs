using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public sealed class CodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeMethod method;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    public CodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass",
            Kind = CodeClassKind.RequestBuilder
        };
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
    [Fact]
    public void WritesRequestBuilder()
    {
        method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesRequestBodiesThrowOnNullHttpMethod()
    {
        method.Kind = CodeMethodKind.RequestExecutor;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.Kind = CodeMethodKind.RequestGenerator;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesModelFactoryBodyThrowsIfMethodAndNotFactory()
    {
        var parentModel = TestHelper.CreateModelClass(root, "parentModel");
        var childModel = TestHelper.CreateModelClass(root, "childModel");
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                TypeDefinition = new CodeClass
                {
                    Name = "ParseNode",
                },
                IsExternal = true,
            },
            Optional = false,
        });
        Assert.Throws<InvalidOperationException>(() => writer.Write(factoryMethod));
    }
    [Fact]
    public void WritesMethodAsyncDescription()
    {//TODO move to functions
        method.Documentation.Description = MethodDescription;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                Description = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("/**", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("@param ", result);
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("@returns a Promise of", result);
        Assert.Contains("*/", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesMethodSyncDescription()
    {//TODO move to functions
        method.Documentation.Description = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                Description = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("@returns a Promise of", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesMethodDescriptionLink()
    {//TODO move to functions
        method.Documentation.Description = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://foo.org/docs");
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                Description = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("@see {@link", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void Defensive()
    {
        var codeMethodWriter = new CodeMethodWriter(new TypeScriptConventionService());
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
        Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
    }
    [Fact]
    public void WritesReturnType()
    { //TODO move to functions
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains(MethodName, result);
        Assert.Contains(ReturnTypeName, result);
        Assert.Contains("Promise<", result);// async default
        Assert.Contains("| undefined", result);// nullable default
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    { //TODO move to functions
        method.ReturnType.IsNullable = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("| undefined", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    { //TODO move to functions
        method.IsAsync = false;
        writer.Write(method);
        var result = tw.ToString();
        Assert.DoesNotContain("Promise<", result);
        Assert.DoesNotContain("async", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesPublicMethodByDefault()
    { //TODO move to functions
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("public ", result);// public default
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesPrivateMethod()
    { //TODO move to functions
        method.Access = AccessModifier.Private;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("private ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public void WritesProtectedMethod()
    {
        method.Access = AccessModifier.Protected;
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("protected ", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void FailsOnCodeClassParent()
    {
        method.AddAccessedProperty();
        method.Kind = CodeMethodKind.Getter;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesWithUrl()
    {
        method.Kind = CodeMethodKind.RawUrlBuilder;
        method.IsAsync = false;
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        method.AddParameter(new CodeParameter
        {
            Name = "rawUrl",
            Kind = CodeParameterKind.RawUrl,
            Type = new CodeType
            {
                Name = "string"
            },
        });
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
    [Fact]
    public void WritesConstructorWithEnumValue()
    {//TODO move to functions
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "1024x1024";
        var propName = "size";
        var codeEnum = new CodeEnum
        {
            Name = "pictureSize"
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"this.{propName.ToFirstCharacterLowerCase()} = {codeEnum.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
    }
    [Fact]
    public void WritesNameMapperMethod()
    {
        method.Kind = CodeMethodKind.QueryParametersMapper;
        method.IsAsync = false;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "select",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24select",
            Type = new CodeType
            {
                Name = "string",
            }
        },
        new CodeProperty
        {
            Name = "expand",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24expand",
            Type = new CodeType
            {
                Name = "string",
            }
        },
        new CodeProperty
        {
            Name = "filter",
            Kind = CodePropertyKind.QueryParameter,
            SerializationName = "%24filter",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        method.AddParameter(new CodeParameter
        {
            Kind = CodeParameterKind.QueryParametersMapperParameter,
            Name = "originalName",
            Type = new CodeType
            {
                Name = "string",
            }
        });
        Assert.Throws<InvalidOperationException>(() => writer.Write(method));
    }
}
