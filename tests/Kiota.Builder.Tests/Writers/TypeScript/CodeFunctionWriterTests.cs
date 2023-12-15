using System;
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
        var factoryFunction = root.AddFunction(new CodeFunction(factoryMethod)).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        var factoryFunction = root.AddFunction(new CodeFunction(factoryMethod)).First();

        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        var factoryFunction = root.AddFunction(new CodeFunction(factoryMethod)).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        var factoryFunction = root.AddFunction(new CodeFunction(factoryMethod)).First();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
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
        writer.Write(deserializerFunction);
        var result = tw.ToString();
        Assert.Contains("getStringValue", result);
        Assert.Contains("getCollectionOfPrimitiveValues", result);
        Assert.Contains("getCollectionOfObjectValues", result);
        Assert.Contains("getEnumValue", result);
        Assert.Contains("definedInParent", result, StringComparison.OrdinalIgnoreCase);
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
        var factoryMethod = errorClass.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "Error4XX",
                TypeDefinition = errorClass,
            },
            IsStatic = true,
        }).First();
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
            Name = "ODataError",
            Kind = CodeClassKind.Model,
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
        var coreProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "HttpCore",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        method.DeserializerModules = new() { "com.microsoft.kiota.serialization.Deserializer" };
        method.SerializerModules = new() { "com.microsoft.kiota.serialization.Serializer" };
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("constructor", result);
        Assert.Contains("registerDefaultSerializer", result);
        Assert.Contains("registerDefaultDeserializer", result);
        Assert.Contains($"[\"baseurl\"] = core.baseUrl", result);
        Assert.Contains($"baseUrl = \"{method.BaseUrl}\"", result);
    }
    [Fact]
    public void WritesApiConstructorWithBackingStore()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.ClientConstructor;
        var coreProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "core",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "HttpCore",
                IsExternal = true,
            }
        }).First();
        method.AddParameter(new CodeParameter
        {
            Name = "core",
            Kind = CodeParameterKind.RequestAdapter,
            Type = coreProp.Type,
        });
        var backingStoreParam = new CodeParameter
        {
            Name = "backingStore",
            Kind = CodeParameterKind.BackingStore,
            Type = new CodeType
            {
                Name = "BackingStore",
                IsExternal = true,
            }
        };
        method.AddParameter(backingStoreParam);
        var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tempWriter.SetTextWriter(tw);
        tempWriter.Write(method);
        var result = tw.ToString();
        Assert.Contains("enableBackingStore", result);
    }
    [Fact]
    public void WritesDefaultValuesInFactory()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Constructor;
        method.IsAsync = false;
        var defaultValue = "someVal";
        var propName = "propWithDefaultValue";
        parentClass.Kind = CodeClassKind.RequestBuilder;
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
        method.AddParameter(new CodeParameter
        {
            Name = "pathParameters",
            Kind = CodeParameterKind.PathParameters,
            Type = new CodeType
            {
                Name = "Map<string,string>"
            }
        });
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains($"this.{propName} = {defaultValue}", result);
    }
    [Fact]
    public void DoesNotWriteConstructorWithDefaultFromComposedType()
    {
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ODataError",
            Kind = CodeClassKind.Model,
        }).First();
        var method = TestHelper.CreateMethod(parentClass, MethodName, ReturnTypeName);
        method.Kind = CodeMethodKind.Constructor;
        var defaultValue = "\"Test Value\"";
        var propName = "size";
        var unionTypeWrapper = root.AddClass(new CodeClass
        {
            Name = "UnionTypeWrapper",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionTypeWrapper",
            },
            DiscriminatorInformation = new()
            {
                DiscriminatorPropertyName = "@odata.type",
            },
        }).First();
        parentClass.AddProperty(new CodeProperty
        {
            Name = propName,
            DefaultValue = defaultValue,
            Kind = CodePropertyKind.Custom,
            Type = new CodeType { TypeDefinition = unionTypeWrapper }
        });
        var sType = new CodeType
        {
            Name = "string",
        };
        var arrayType = new CodeType
        {
            Name = "array",
        };
        unionTypeWrapper.OriginalComposedType.AddType(sType);
        unionTypeWrapper.OriginalComposedType.AddType(arrayType);

        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("constructor", result);
        Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
    }
}
