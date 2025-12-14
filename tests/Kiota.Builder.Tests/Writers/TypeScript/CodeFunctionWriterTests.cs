using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests.OpenApiSampleFiles;
using Kiota.Builder.Writers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static Kiota.Builder.Refiners.TypeScriptRefiner;

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
    private readonly HttpClient _httpClient = new();
    private readonly List<string> _tempFiles = new();

    public CodeFunctionWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    public void Dispose()
    {
        foreach (var file in _tempFiles)
            File.Delete(file);
        _httpClient.Dispose();
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WritesAutoGenerationStartAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("/* eslint-disable */", result);
        Assert.DoesNotContain("/* tslint:disable */", result);
    }
    [Fact]
    public async Task WritesAutoGenerationEndAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("/* eslint-enable */", result); //written by code end block writer
        Assert.DoesNotContain("/* tslint:enable */", result);
    }

    [Fact]
    public async Task WritesModelFactoryBodyAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var modelInterface = root.FindChildByName<CodeInterface>("childModel");
        Assert.NotNull(modelInterface);
        var parentNS = modelInterface.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var factoryFunction = parentNS.FindChildByName<CodeFunction>("createParentModelFromDiscriminatorValue", false);
        parentNS.TryAddCodeFile("foo", factoryFunction);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("const mappingValueNode = parseNode?.getChildNode(\"@odata.type\")", result);
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
    public async Task DoesntWriteFactorySwitchOnMissingParameterAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task DoesntWriteFactorySwitchOnEmptyPropertyNameAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task DoesntWriteFactorySwitchOnEmptyMappingsAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task WritesInheritedDeSerializerBodyAsync()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass", true);
        var inheritedClass = parentClass.BaseClass;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task WritesDeSerializerBodyAsync()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass");
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task WritesDeSerializerBodyWithDefaultValueAsync()
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
        var propertyEnum = new CodeEnum
        {
            Name = "EnumTypeWithOption",
            Parent = parentClass,
        };
        var enumOption = new CodeEnumOption() { Name = "SomeOption" };
        propertyEnum.AddOption(enumOption);
        var codeNamespace = parentClass.Parent as CodeNamespace;
        codeNamespace.AddEnum(propertyEnum);
        parentClass.AddProperty(new CodeProperty
        {
            Name = "propWithDefaultEnum",
            DefaultValue = enumOption.Name,
            Type = new CodeType
            {
                Name = "EnumTypeWithOption",
                TypeDefinition = propertyEnum,
            }
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var deserializerFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(deserializerFunction);
        var parentNS = deserializerFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", deserializerFunction);
        writer.Write(deserializerFunction);
        var result = tw.ToString();
        Assert.Contains("?? \"Test Value\"", result);
        Assert.Contains("?? EnumTypeWithOptionObject.SomeOption", result);
    }
    [Fact]
    public async Task WritesSerializerBodyEnumCollectionAsync()
    {
        var parentClass = TestHelper.CreateModelClass(root, "parentClass");
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        var propName = "propWithDefaultValue";
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
            },
        });
        var propertyEnum = new CodeEnum
        {
            Name = "EnumTypeWithOption",
            Parent = parentClass,
        };
        var enumOption = new CodeEnumOption() { Name = "SomeOption" };
        propertyEnum.AddOption(enumOption);
        var codeNamespace = parentClass.Parent as CodeNamespace;
        codeNamespace.AddEnum(propertyEnum);
        parentClass.AddProperty(new CodeProperty
        {
            Name = "propWithDefaultEnum",
            DefaultValue = enumOption.Name,
            Type = new CodeType
            {
                TypeDefinition = propertyEnum,
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            }
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializerFunction = root.FindChildByName<CodeFunction>($"serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializerFunction);
        var parentNS = serializerFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        var complexTypeDefinition = root.FindChildByName<CodeInterface>("SomeComplexType");
        Assert.NotNull(complexTypeDefinition);
        parentNS.TryAddCodeFile("foo", serializerFunction, parentClass, complexTypeDefinition);
        writer.Write(serializerFunction);
        var result = tw.ToString();
        Assert.Contains("writeCollectionOfEnumValues<EnumTypeWithOption>(\"propWithDefaultEnum\"", result);
        Assert.Contains("?? [EnumTypeWithOptionObject.SomeOption]", result);
    }
    [Fact]
    public async Task WritesInheritedSerializerBodyAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass", true);
        var inheritedClass = parentClass.BaseClass;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(generationConfiguration, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($"serialize{inheritedClass.Name.ToFirstCharacterUpperCase()}(writer, parentClass, isSerializingDerivedType)", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task WritesSerializerBodyAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        Assert.Contains("writer.writeAdditionalData", result);
        Assert.Contains($"if (!{parentClass.Name.ToFirstCharacterLowerCase()} || isSerializingDerivedType) {{ return; }}", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WritesSerializerBodyWithDiscriminatorAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        parentClass.DiscriminatorInformation.AddDiscriminatorMapping("ns.childclass", new CodeType
        {
            Name = "childClass",
            TypeDefinition = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "childClass", true),
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "odataType",
            Kind = CodePropertyKind.Custom,
            Type = new CodeType
            {
                Name = "string",
            },
            SerializationName = "@odata.type",
        });
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains("switch (parentClass.odataType) {", result);
        Assert.Contains("case \"ns.childclass\":", result);
        Assert.Contains("serializeChildClass(writer, parentClass, true);", result);
    }

    [Fact]
    public async Task WritesSerializerBodyWithDefaultAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task DoesntWriteReadOnlyPropertiesInSerializerBodyAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
    public async Task AddsUsingsForErrorTypesForRequestExecutorAsync()
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
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);

        var declaration = requestBuilder.StartBlock;
        var serializeFunction = subNS.FindChildByName<CodeFunction>("createError4XXFromDiscriminatorValue");
        Assert.NotNull(serializeFunction);
        Assert.Contains("createError4XXFromDiscriminatorValue", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task WritesMessageOverrideOnPrimaryAsync()
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

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        Assert.Contains(
            "serializationWriterFactory = requestAdapter.getSerializationWriterFactory() as SerializationWriterFactoryRegistry",
            result);
        Assert.Contains("parseNodeFactoryRegistry = requestAdapter.getParseNodeFactory() as ParseNodeFactoryRegistry",
            result);
        Assert.Contains("const backingStoreFactory = requestAdapter.getBackingStoreFactory();", result);
        Assert.Contains("serializationWriterFactory.registerDefaultSerializer", result);
        Assert.Contains("parseNodeFactoryRegistry.registerDefaultDeserializer", result);
        Assert.Contains($"baseUrl = \"{method.BaseUrl}\"", result);
        Assert.Contains($"\"baseurl\": requestAdapter.baseUrl", result);
        Assert.Contains($"apiClientProxifier<", result);
        Assert.Contains($"pathParameters", result);
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
    [Fact]
    public void WritesDeprecationInformationFromBuilder()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Name = "NewAwesomeMethod";// new method replacement
        method.Deprecation = new("This method is obsolete. Use {TypeName} instead.", IsDeprecated: true, TypeReferences: new() { { "TypeName", new CodeType { TypeDefinition = method, IsExternal = false } } });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("This method is obsolete. Use NewAwesomeMethod instead.", result);
    }
    [Fact]
    public void WritesDefensiveStatements()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        parentClass.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        parentClass.DiscriminatorInformation.AddDiscriminatorMapping("string", new CodeType() { Name = "string" });
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Type = new CodeType()
            {
                Name = "string",
                IsNullable = false,
            },
        });
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Name = "NewAwesomeMethod";// new method replacement
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("cannot be undefined", result);
    }
    [Fact]
    public void DoesNotWriteDefensiveStatementsForBooleanParameters()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Type = new CodeType()
            {
                Name = "boolean",
                IsNullable = false,
            },
            Optional = false,
        });
        method.Kind = CodeMethodKind.Factory;
        method.IsStatic = true;
        method.Name = "NewAwesomeMethod";// new method replacement
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("cannot be undefined", result);
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
        Assert.Contains("@returns {Promise<", result);
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
        Assert.DoesNotContain("@returns {Promise<", result);
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
        });
        method.ReturnType.IsNullable = false;
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("| undefined", result[result.IndexOf(": Promise<", StringComparison.OrdinalIgnoreCase)..]);
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
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
            OriginalClass = new CodeClass() { Name = "SomeInterface" }
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
        method.AddParameter(new CodeParameter
        {
            Name = "isDerivedSerialization",
            Type = new CodeType
            {
                Name = "boolean",
            },
            Kind = CodeParameterKind.SerializingDerivedType,
        });
        var function = new CodeFunction(method);
        root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.DoesNotContain("export ", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }
    [Fact]
    public async Task WritesConstructorWithEnumValueAsync()
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

        codeEnum.AddOption(
            new CodeEnumOption
            {
                Name = "256x256",
                SerializationName = "256x256"
            },
            new CodeEnumOption
            {
                Name = "512x512",
                SerializationName = "512x512"
            },
            new CodeEnumOption
            {
                Name = "1024x1024",
                SerializationName = "1024x1024"
            });
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = codeEnum }
        });
        method.IsStatic = true;
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($" ?? {codeEnum.CodeEnumObject.Name.ToFirstCharacterUpperCase()}.{defaultValue.CleanupSymbolName()}", result);//ensure symbol is cleaned up
    }
    [Fact]
    public async Task Writes_UnionOfPrimitiveValues_FactoryFunctionAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, UnionOfPrimitiveValuesSample.Yaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Primitives", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Primitives", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.primitives");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("primitivesRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        /*
        \/**
        * Creates a new instance of the appropriate class based on discriminator value
        * @returns {ValidationError_errors_value}
        *\/
           export function createPrimitivesFromDiscriminatorValue(parseNode: ParseNode | undefined) : Primitives | undefined {
                return parseNode?.getNumberValue() ?? parseNode?.getStringValue();
            }
         */

        // Test Factory function
        var factoryFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && GetOriginalComposedType(function.OriginalLocalMethod.ReturnType) is not null);
        Assert.True(factoryFunction is not null);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("return parseNode?.getNumberValue() ?? parseNode?.getStringValue();", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public async Task Writes_UnionOfObjects_FactoryMethodAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, PetsUnion.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Pets", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Pets", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.pets");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("petsRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Serializer function
        var factoryFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && function.OriginalLocalMethod.Kind == CodeMethodKind.Factory);
        Assert.True(factoryFunction is not null);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("if (mappingValue)", result);
        Assert.Contains("case \"Cat\":", result);
        Assert.Contains("return deserializeIntoCat;", result);
        Assert.Contains("case \"Dog\":", result);
        Assert.Contains("return deserializeIntoDog;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public async Task Writes_UnionOfPrimitiveValues_SerializerFunctionAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, UnionOfPrimitiveValuesSample.Yaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Primitives", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Primitives", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.primitives");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("primitivesRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Serializer function
        var serializerFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && GetOriginalComposedType(function.OriginalLocalMethod.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null)) is not null);
        Assert.True(serializerFunction is not null);
        writer.Write(serializerFunction);
        var serializerFunctionStr = tw.ToString();
        Assert.Contains("return", serializerFunctionStr);
        Assert.Contains("typeof primitives === \"number\"", serializerFunctionStr);
        Assert.Contains("typeof primitives === \"string\"", serializerFunctionStr);
        AssertExtensions.CurlyBracesAreClosed(serializerFunctionStr, 1);
    }

    [Fact]
    public async Task Writes_UnionOfObjects_SerializerFunctionsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, PetsUnion.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "Pets", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("Pets", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.pets");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("petsRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Serializer function
        var serializerFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && function.OriginalLocalMethod.Kind == CodeMethodKind.Serializer);
        Assert.True(serializerFunction is not null);
        writer.Write(serializerFunction);
        var serializerFunctionStr = tw.ToString();
        Assert.Contains("return", serializerFunctionStr);
        Assert.Contains("switch", serializerFunctionStr);
        Assert.Contains("case \"Cat\":", serializerFunctionStr);
        Assert.Contains("case \"Dog\":", serializerFunctionStr);
        Assert.Contains("break", serializerFunctionStr);
        AssertExtensions.CurlyBracesAreClosed(serializerFunctionStr, 1);
    }

    [Fact]
    public async Task Writes_CodeIntersectionType_FactoryMethodAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, CodeIntersectionTypeSampleYml.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "FooBar", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("FooBar", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.foobar");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("foobarRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Factory Function
        var factoryFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && function.OriginalLocalMethod.Kind == CodeMethodKind.Factory);
        Assert.True(factoryFunction is not null);
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("export function createFooBarFromDiscriminatorValue(", result);
        Assert.Contains("return deserializeIntoFooBar;", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public async Task Writes_CodeIntersectionType_DeserializerFunctionsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, CodeIntersectionTypeSampleYml.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "FooBar", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("FooBar", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.foobar");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("foobarRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Deserializer function
        var deserializerFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && function.OriginalLocalMethod.Kind == CodeMethodKind.Deserializer);
        Assert.True(deserializerFunction is not null);
        writer.Write(deserializerFunction);
        var serializerFunctionStr = tw.ToString();
        Assert.Contains("...deserializeIntoBar(fooBar as Bar),", serializerFunctionStr);
        Assert.Contains("...deserializeIntoFoo(fooBar as Foo),", serializerFunctionStr);
        AssertExtensions.CurlyBracesAreClosed(serializerFunctionStr, 1);
    }

    [Fact]
    public async Task Writes_CodeIntersectionType_SerializerFunctionsAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, CodeIntersectionTypeSampleYml.OpenApiYaml);
        var mockLogger = new Mock<ILogger<KiotaBuilder>>();
        var builder = new KiotaBuilder(mockLogger.Object, new GenerationConfiguration { ClientClassName = "FooBar", Serializers = ["none"], Deserializers = ["none"] }, _httpClient);
        await using var fs = new FileStream(tempFilePath, FileMode.Open);
        var document = await builder.CreateOpenApiDocumentAsync(fs);
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var codeModel = builder.CreateSourceModel(node);
        var rootNS = codeModel.FindNamespaceByName("ApiSdk");
        Assert.NotNull(rootNS);
        var clientBuilder = rootNS.FindChildByName<CodeClass>("FooBar", false);
        Assert.NotNull(clientBuilder);
        var constructor = clientBuilder.Methods.FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor));
        Assert.NotNull(constructor);
        Assert.Empty(constructor.SerializerModules);
        Assert.Empty(constructor.DeserializerModules);
        await ILanguageRefiner.RefineAsync(generationConfiguration, rootNS);
        Assert.NotNull(rootNS);
        var modelsNS = rootNS.FindNamespaceByName("ApiSdk.foobar");
        Assert.NotNull(modelsNS);
        var modelCodeFile = modelsNS.FindChildByName<CodeFile>("foobarRequestBuilder", false);
        Assert.NotNull(modelCodeFile);

        // Test Serializer function
        var serializerFunction = modelCodeFile.GetChildElements().FirstOrDefault(x => x is CodeFunction function && function.OriginalLocalMethod.Kind == CodeMethodKind.Serializer);
        Assert.True(serializerFunction is not null);
        writer.Write(serializerFunction);
        var serializerFunctionStr = tw.ToString();
        Assert.Contains("serializeBar(writer, fooBar as Bar);", serializerFunctionStr);
        Assert.Contains("serializeFoo(writer, fooBar as Foo);", serializerFunctionStr);
        AssertExtensions.CurlyBracesAreClosed(serializerFunctionStr, 1);
    }

    [Fact]
    public async Task Writes_CodeUnionBetweenObjectsAndPrimitiveTypes_SerializerAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;

        var modelNameSpace = root.AddNamespace($"{root.Name}.models");
        var composedType = new CodeUnionType { Name = "Union" };
        composedType.AddType(new CodeType { Name = "string" }, new CodeType { Name = "int" },
            new CodeType
            {
                Name = "ArrayOfObjects",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = TestHelper.CreateModelClass(modelNameSpace, "ArrayOfObjects")
            },
            new CodeType
            {
                Name = "SingleObject",
                TypeDefinition = TestHelper.CreateModelClass(modelNameSpace, "SingleObject")
            });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = composedType
        });


        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();

        Assert.Contains("typeof parentClass.property === \"string\"", result);
        Assert.Contains("writer.writeStringValue(\"property\", parentClass.property as string);", result);
        Assert.Contains("typeof parentClass.property === \"number\"", result);
        Assert.Contains("writer.writeNumberValue(\"property\", parentClass.property as number);", result);
        Assert.Contains(
            "writer.writeCollectionOfObjectValues<ArrayOfObjects>(\"property\", parentClass.property as ArrayOfObjects[] | undefined | null",
            result);
        Assert.Contains(
            "writer.writeObjectValue<SingleObject>(\"property\", parentClass.property as SingleObject | undefined | null",
            result);
        Assert.Contains("writeStringValue", result);
        Assert.Contains("writeCollectionOfPrimitiveValues", result);
        Assert.Contains("writeCollectionOfObjectValues", result);
        Assert.Contains("serializeSomeComplexType", result);
        Assert.Contains("writeEnumValue", result);
        Assert.Contains("writer.writeAdditionalData", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Writes_CodeUnionBetweenObjectsAndPrimitiveTypes_DeserializerAsync()
    {
        var generationConfiguration = new GenerationConfiguration { Language = GenerationLanguage.TypeScript };
        var parentClass = TestHelper.CreateModelClassInModelsNamespace(generationConfiguration, root, "parentClass");
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;

        var modelNameSpace = root.AddNamespace($"{root.Name}.models");
        var composedType = new CodeUnionType { Name = "Union" };
        composedType.AddType(new CodeType { Name = "string" }, new CodeType { Name = "int" },
            new CodeType
            {
                Name = "ArrayOfObjects",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = TestHelper.CreateModelClass(modelNameSpace, "ArrayOfObjects")
            },
            new CodeType
            {
                Name = "SingleObject",
                TypeDefinition = TestHelper.CreateModelClass(modelNameSpace, "SingleObject")
            });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = composedType
        });

        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"DeserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        Assert.NotNull(serializeFunction);
        var parentNS = serializeFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", serializeFunction);
        writer.Write(serializeFunction);
        var result = tw.ToString();

        Assert.Contains("\"property\": n => { parentClass.property = n.getCollectionOfObjectValues<ArrayOfObjects>(createArrayOfObjectsFromDiscriminatorValue) ?? n.getNumberValue() ?? n.getObjectValue<SingleObject>(createSingleObjectFromDiscriminatorValue) ?? n.getStringValue(); }", result);
    }

    [Fact]
    public void WritesByteArrayPropertyDeserialization()
    {
        var intfc = new CodeInterface
        {
            Name = "SomeInterface",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass() { Name = "SomeClass" }
        };
        var deserializationMethod = new CodeMethod
        {
            Name = "deserialize",
            ReturnType = new CodeType { Name = "SomeInterface" },
            Access = AccessModifier.Public,
            IsAsync = true,
            IsStatic = true,
            Kind = CodeMethodKind.Deserializer,
        };
        deserializationMethod.AddParameter(new CodeParameter
        {
            Name = "model",
            Type = new CodeType { TypeDefinition = intfc }
        });
        intfc.OriginalClass.AddMethod(deserializationMethod);
        var prop = new CodeProperty
        {
            Name = "property",
            Type = new CodeType { Name = "base64" }
        };
        intfc.AddProperty(prop);
        var function = new CodeFunction(deserializationMethod);
        var codeFile = new CodeFile
        {
            Name = "someFile",
        };
        codeFile.AddElements(function);
        writer.Write(function);
        var result = tw.ToString();
        Assert.Contains("\"property\": n => { model.property = n.getByteArrayValue(); }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void WritesFactoryMethodBodyForErrorClassWithMessage()
    {
        // Create an error class
        var errorClass = root.AddClass(new CodeClass
        {
            Name = "SomeError",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true
        }).First();

        // Create the factory method with message parameter
        var factoryMethod = errorClass.AddMethod(new CodeMethod
        {
            Name = "createFromDiscriminatorValueWithMessage",
            Kind = CodeMethodKind.FactoryWithErrorMessage,
            ReturnType = new CodeType
            {
                Name = "SomeError",
                TypeDefinition = errorClass,
                IsNullable = true
            },
            IsStatic = true,
        }).First();

        // Add parseNode parameter
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });

        // Add message parameter
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "message",
            Kind = CodeParameterKind.ErrorMessage,
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true,
            },
            Optional = true,
        });

        var function = new CodeFunction(factoryMethod);
        var codeFile = root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();

        // Should call the constructor with message
        Assert.Contains("return new SomeError(message);", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void WritesFactoryMethodBodyForErrorClassWithoutMessage()
    {
        // Create an error class
        var errorClass = root.AddClass(new CodeClass
        {
            Name = "SomeError",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true
        }).First();

        // Create the factory method without message parameter
        var factoryMethod = errorClass.AddMethod(new CodeMethod
        {
            Name = "createFromDiscriminatorValueWithMessage",
            Kind = CodeMethodKind.FactoryWithErrorMessage,
            ReturnType = new CodeType
            {
                Name = "SomeError",
                TypeDefinition = errorClass,
                IsNullable = true
            },
            IsStatic = true,
        }).First();

        // Add only parseNode parameter (no message parameter)
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });

        var function = new CodeFunction(factoryMethod);
        var codeFile = root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();

        // Should call the parameterless constructor
        Assert.Contains("return new SomeError();", result);
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public void DoesNotWriteSpecialFactoryMethodForNonErrorClasses()
    {
        // Create a regular class
        var regularClass = root.AddClass(new CodeClass
        {
            Name = "SomeModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false
        }).First();

        // Create the factory method
        var factoryMethod = regularClass.AddMethod(new CodeMethod
        {
            Name = "createFromDiscriminatorValueWithMessage",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "SomeModel",
                TypeDefinition = regularClass,
                IsNullable = true
            },
            IsStatic = true,
        }).First();

        // Add parameters
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode",
                IsExternal = true,
            },
            Optional = false,
        });

        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "message",
            Kind = CodeParameterKind.ErrorMessage,
            Type = new CodeType
            {
                Name = "string",
                IsExternal = true,
            },
            Optional = true,
        });

        var function = new CodeFunction(factoryMethod);
        var codeFile = root.TryAddCodeFile("foo", function);
        writer.Write(function);
        var result = tw.ToString();

        // Should not use special error handling for non-error classes
        Assert.DoesNotContain("new SomeModel(message)", result);
        // Should continue with normal factory method logic - the exact output varies based on setup but should not be the error handling path
        AssertExtensions.CurlyBracesAreClosed(result, 1);
    }

    [Fact]
    public async Task WritesOneOfWithInheritanceDeserializationAsync()
    {
        // Create base class "Device"
        var baseClass = TestHelper.CreateModelClass(root, "Device");
        baseClass.AddProperty(new CodeProperty
        {
            Name = "deviceId",
            Type = new CodeType { Name = "string" },
            Kind = CodePropertyKind.Custom,
        });

        // Create derived class "ManagedPrivilegedDevice" that inherits from "Device"
        var derivedClass = TestHelper.CreateModelClass(root, "ManagedPrivilegedDevice");
        derivedClass.StartBlock.Inherits = new CodeType
        {
            Name = "Device",
            TypeDefinition = baseClass,
        };
        derivedClass.AddProperty(new CodeProperty
        {
            Name = "privilegeLevel",
            Type = new CodeType { Name = "string" },
            Kind = CodePropertyKind.Custom,
        });

        // Create a model with a oneOf property containing both types
        var parentClass = TestHelper.CreateModelClass(root, "Container");
        var composedType = new CodeUnionType { Name = "DeviceUnion" };

        // Add types in alphabetical order: Device comes before ManagedPrivilegedDevice
        // But ManagedPrivilegedDevice should be checked first because it's derived from Device
        composedType.AddType(
            new CodeType { Name = "Device", TypeDefinition = baseClass },
            new CodeType { Name = "ManagedPrivilegedDevice", TypeDefinition = derivedClass }
        );

        parentClass.AddProperty(new CodeProperty
        {
            Name = "deviceProperty",
            Type = composedType,
            Kind = CodePropertyKind.Custom,
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var deserializerFunction = root.FindChildByName<CodeFunction>($"deserializeIntoContainer");
        Assert.NotNull(deserializerFunction);
        var parentNS = deserializerFunction.GetImmediateParentOfType<CodeNamespace>();
        Assert.NotNull(parentNS);
        parentNS.TryAddCodeFile("foo", deserializerFunction);
        writer.Write(deserializerFunction);
        var result = tw.ToString();

        // The generated code should check ManagedPrivilegedDevice first (derived class)
        // before checking Device (base class), regardless of alphabetical order
        // This ensures proper deserialization when the derived type is received
        var managedDeviceIndex = result.IndexOf("createManagedPrivilegedDeviceFromDiscriminatorValue", StringComparison.Ordinal);
        var deviceIndex = result.IndexOf("createDeviceFromDiscriminatorValue", StringComparison.Ordinal);

        Assert.True(managedDeviceIndex > 0, "Should contain ManagedPrivilegedDevice deserialization");
        Assert.True(deviceIndex > 0, "Should contain Device deserialization");
        Assert.True(managedDeviceIndex < deviceIndex, "ManagedPrivilegedDevice should appear before Device in the deserialization chain");
    }
}

