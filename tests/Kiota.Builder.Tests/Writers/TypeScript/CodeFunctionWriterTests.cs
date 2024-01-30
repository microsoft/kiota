using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public sealed class CodeFunctionWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";

    public CodeFunctionWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
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
    public async Task WritesAutoGenerationStart()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("/* eslint-disable */", result);
        Assert.DoesNotContain("/* tslint:disable */", result);
    }
    [Fact]
    public async Task WritesAutoGenerationEnd()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("/* eslint-enable */", result); //written by code end block writer
        Assert.DoesNotContain("/* tslint:enable */", result);
    }

    [Fact]
    public async Task WritesModelFactoryBody()
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
            IsStatic = true,
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var modelInterface = root.FindChildByName<CodeInterface>("childModel");
        Assert.NotNull(modelInterface);
        var parentNS = modelInterface.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var factoryFunction = parentNS.FindChildByName<CodeFunction>("createParentModelFromDiscriminatorValue", false);
        parentNS.TryAddCodeFile("foo", factoryFunction);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.Contains("if (mappingValueNode) {", result);
        Assert.Contains("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.Contains("if (mappingValue) {", result);
        Assert.Contains("switch (mappingValue) {", result);
        Assert.Contains("case \"ns.childmodel\":", result);
        Assert.Contains("return deserializeIntoChildModel;", result);
        Assert.Contains("return deserializeIntoParentModel;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public async Task DoesntWriteFactorySwitchOnMissingParameter()
    {
        var parentModel = TestHelper.CreateModelClass(root, "parentModel");
        var childModel = TestHelper.CreateModelClass(root, "childModel");
        childModel.StartBlock.Inherits = new CodeType
        {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        });
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var modelInterface = root.FindChildByName<CodeInterface>("childModel");
        Assert.NotNull(modelInterface);
        var parentNS = modelInterface.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var factoryFunction = parentNS.FindChildByName<CodeFunction>("createParentModelFromDiscriminatorValue", false);
        parentNS.TryAddCodeFile("foo", factoryFunction);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return deserializeIntoChildModel;", result);
        Assert.Contains("return deserializeIntoParentModel;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public async Task DoesntWriteFactorySwitchOnEmptyPropertyName()
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
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType
        {
            Name = "childModel",
            TypeDefinition = childModel,
        });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = string.Empty;
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var modelInterface = root.FindChildByName<CodeInterface>("childModel");
        Assert.NotNull(modelInterface);
        var parentNS = modelInterface.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var factoryFunction = parentNS.FindChildByName<CodeFunction>("createParentModelFromDiscriminatorValue", false);
        parentNS.TryAddCodeFile("foo", factoryFunction);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return new deserializeIntoChildModel;", result);
        Assert.Contains("return deserializeIntoParentModel;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public async Task DoesntWriteFactorySwitchOnEmptyMappings()
    {
        var parentModel = TestHelper.CreateModelClass(root, "parentModel");
        var factoryMethod = parentModel.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
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
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var modelInterface = root.FindChildByName<CodeInterface>("parentModel");
        Assert.NotNull(modelInterface);
        var parentNS = modelInterface.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var factoryFunction = parentNS.FindChildByName<CodeFunction>("createParentModelFromDiscriminatorValue", false);
        parentNS.TryAddCodeFile("foo", factoryFunction);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return deserializeIntoChildModel;", result);
        Assert.Contains("return deserializeIntoParentModel;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public async Task WritesInheritedDeSerializerBody()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass", true);
        var inheritedClass = parentClass.BaseClass;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($"...deserializeInto{inheritedClass.Name.ToFirstCharacterUpperCase()}", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task WritesDeSerializerBody()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass");
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var deserializerFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(deserializerFunction);
        var parentNS = deserializerFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", deserializerFunction);
        writer.Write(deserializerFunction);
        var result = tw.ToString();
        Assert.Contains("getStringValue", result);
        Assert.Contains("getCollectionOfPrimitiveValues", result);
        Assert.Contains("getCollectionOfObjectValues", result);
        Assert.Contains("getEnumValue", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task WritesDeSerializerBodyWithDefaultValue()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass");
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        var defaultValue = "\"Test Value\"";
        var propName = "propWithDefaultValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
            },
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var deserializerFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(deserializerFunction);
        var parentNS = deserializerFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", deserializerFunction);
        writer.Write(deserializerFunction);
        var result = tw.ToString();
        Assert.Contains("?? \"Test Value\"", result);
    }
    [Fact]
    public async Task WritesInheritedSerializerBody()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        var inheritedClass = parentClass.BaseClass;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($"serialize{inheritedClass.Name.ToFirstCharacterUpperCase()}(writer, parentClass)", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task WritesSerializerBody()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains("writeStringValue", result);
        Assert.Contains("writeCollectionOfPrimitiveValues", result);
        Assert.Contains("writeCollectionOfObjectValues", result);
        Assert.Contains("serializeSomeComplexType", result);
        Assert.Contains("writeEnumValue", result);
        Assert.Contains($"writer.writeAdditionalData", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WritesSerializerBodyWithDefault()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        var defaultValue = "\"Test Value\"";
        var propName = "propWithDefaultValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
            },
        });
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains("?? \"Test Value\"", result);
    }

    [Fact]
    public async Task DoesntWriteReadOnlyPropertiesInSerializerBody()
    {
        var model = TestHelper.CreateModelClass(root, "TestModel");
        model.AddProperty(new CodeProperty
        {
            Name = "ReadOnlyProperty",
            ReadOnly = true,
            Type = new CodeType
            {
                Name = "string",
            },
        });

        model.AddProperty(new CodeProperty
        {
            Name = "someProperty",
            Type = new CodeType
            {
                Name = "string",
            },
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>("SerializeTestModel");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("readOnlyProperty", result);
        Assert.Contains("someProperty", result);
    }

    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutor()
    {
        var requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        var errorClass = TestHelper.CreateModelClass(subNS, "Error4XX");
        errorClass.IsErrorDefinition = true;
        errorClass.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "Error4XX",
                TypeDefinition = errorClass,
            },
            IsStatic = true,
        });
        var requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();
        requestExecutor.AddErrorMapping("4XX", new CodeType
        {
            Name = "Error4XX",
            TypeDefinition = errorClass,
        });
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = requestBuilder.StartBlock;
        var serializeFunction = subNS.FindChildByName<CodeFunction>("createError4XXFromDiscriminatorValue");
        Assert.NotNull(serializeFunction);
        Assert.Contains("createError4XXFromDiscriminatorValue", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task WritesMessageOverrideOnPrimary()
    {
        // Given
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        parentClass.IsErrorDefinition = true;
        parentClass.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.Deserializer,
            Name = "deserializer",
            ReturnType = new CodeType
            {
                Name = "Dictionary",
            },
        }, new CodeMethod
        {
            Name = "Serializer",
            Kind = CodeMethodKind.Serializer,
            ReturnType = new CodeType
            {
                Name = "void",
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "prop1",
            Kind = CodePropertyKind.Custom,
            IsPrimaryErrorMessage = true,
            Type = new CodeType
            {
                Name = "string",
            },
        });

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var function = root.FindChildByName<CodeFunction>("deserializeIntoODataError");
        Assert.NotNull(function);
        var parentNS = function.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", function);

        // When
        writer.Write(function);
        var result = tw.ToString();

        // Then
        Assert.Contains("oDataError.message = oDataError.prop1 ?? \"\"", result);
    }
    [Fact]
    public void WritesApiConstructor()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ApiClient",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.ClientConstructor;
        method.IsAsync = false;
        method.BaseUrl = "https://graph.microsoft.com/v1.0";
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "Dictionary<string, string>",
                IsExternal = true,
            }
        });
        var requestAdapterProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "requestAdapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = requestAdapterProp.Type,
        });
        method.DeserializerModules = ["com.microsoft.kiota.serialization.Deserializer"];
        method.SerializerModules = ["com.microsoft.kiota.serialization.Serializer"];
        method.IsStatic = true;
        root.RemoveChildElement(parentClass);
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function, CodeInterface.FromRequestBuilder(parentClass));
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("registerDefaultSerializer", result);
        Assert.Contains("registerDefaultDeserializer", result);
        Assert.Contains($"baseUrl = \"{method.BaseUrl}\"", result);
        Assert.Contains($"\"baseurl\": requestAdapter.baseUrl", result);
        Assert.Contains($"apiClientProxifier<", result);
        Assert.Contains($"pathParameters", result);
        Assert.Contains($"UriTemplate", result);
    }
    [Fact]
    public void WritesApiConstructorWithBackingStore()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ApiClient",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.ClientConstructor;
        method.IsAsync = false;
        method.BaseUrl = "https://graph.microsoft.com/v1.0";
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "Dictionary<string, string>",
                IsExternal = true,
            }
        });
        var requestAdapterProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "requestAdapter",
            Kind = CodeParameterKind.RequestAdapter,
            Type = requestAdapterProp.Type,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "backingStore",
            Kind = CodeParameterKind.BackingStore,
            Type = new CodeType
            {
                Name = "BackingStore",
                IsExternal = true,
            }
        });
        method.DeserializerModules = ["com.microsoft.kiota.serialization.Deserializer"];
        method.SerializerModules = ["com.microsoft.kiota.serialization.Serializer"];
        method.IsStatic = true;
        root.RemoveChildElement(parentClass);
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function, CodeInterface.FromRequestBuilder(parentClass));
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
    }
    [Fact]
    public void WritesDeprecationInformation()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Deprecation = new("This method is deprecated", DateTimeOffset.Parse("2020-01-01T00:00:00Z", CultureInfo.InvariantCulture), DateTimeOffset.Parse("2021-01-01T00:00:00Z", CultureInfo.InvariantCulture), "v2.0");
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("This method is deprecated", result);
        Assert.Contains("2020-01-01", result);
        Assert.Contains("2021-01-01", result);
        Assert.Contains("v2.0", result);
        Assert.Contains("@deprecated", result);
    }
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";
    [Fact]
    public void WritesMethodAsyncDescription()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Documentation.DescriptionTemplate = MethodDescription;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("/**", result);
        Assert.Contains(MethodDescription, result);
        Assert.Contains("@param ", result);
        Assert.Contains(ParamName, result);
        Assert.Contains(ParamDescription, result);
        Assert.Contains("@returns a Promise of", result);
        Assert.Contains("*/", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void WritesMethodSyncDescription()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("@returns a Promise of", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesMethodDescriptionLink()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Documentation.DescriptionTemplate = MethodDescription;
        method.Documentation.DocumentationLabel = "see more";
        method.Documentation.DocumentationLink = new("https://foo.org/docs");
        method.IsAsync = false;
        var parameter = new CodeParameter
        {
            Documentation = new()
            {
                DescriptionTemplate = ParamDescription,
            },
            Name = ParamName,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(parameter);
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("@see {@link", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void WritesReturnType()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains(MethodName, result);
        Assert.Contains(ReturnTypeName, result);
        Assert.Contains("Promise<", result);// async default
        Assert.Contains("| undefined", result);// nullable default
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public void DoesNotAddUndefinedOnNonNullableReturnType()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        method.ReturnType.IsNullable = false;
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("| undefined", result.Substring(result.IndexOf("Promise<", StringComparison.OrdinalIgnoreCase)));
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void DoesNotAddAsyncInformationOnSyncMethods()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        method.IsAsync = false;
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("Promise<", result);
        Assert.DoesNotContain("async", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void WritesPublicMethodByDefault()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("export ", result);// public default
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void WritesPrivateMethod()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.Access = AccessModifier.Private;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("export ", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void WritesProtectedMethod()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var targetInterface = root.AddInterface(new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsStatic = true;
        method.Access = AccessModifier.Protected;
        method.AddParameter(new CodeParameter
        {
            Name = "someParam",
            Type = new CodeType
            {
                TypeDefinition = targetInterface,
            }
        });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("export ", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public async Task WritesConstructorWithEnumValue()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        method.Kind = CodeMethodKind.Serializer;
        var defaultValue = "1024x1024";
        var propName = "size";
        var codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "pictureSize"
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        method.IsStatic = true;
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($" ?? {codeEnum.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
    }
}
