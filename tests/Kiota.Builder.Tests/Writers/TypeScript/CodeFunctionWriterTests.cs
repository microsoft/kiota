using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Tests;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public class CodeFunctionWriterTests : IDisposable
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

    public CodeFunctionWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = TestHelper.CreateModelClass("parentClass");
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
    public async Task WritesModelFactoryBody()
    {
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
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

        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
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
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
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
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
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
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        var inheritedClass = TestHelper.AddInheritanceClassToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($"...deserializeInto{inheritedClass.Name.ToFirstCharacterUpperCase()}", result);
    }
    [Fact]
    public async Task WritesDeSerializerBody()
    {
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var deserializerFunction = root.FindChildByName<CodeFunction>($"deserializeInto{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(deserializerFunction);
        var result = tw.ToString();
        Assert.Contains("getStringValue", result);
        Assert.Contains("getCollectionOfPrimitiveValues", result);
        Assert.Contains("getCollectionOfObjectValues", result);
        Assert.Contains("getEnumValue", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task WritesInheritedSerializerBody()
    {
        TestHelper.AddSerializationPropertiesToModelClass(parentClass);
        var inheritedClass = TestHelper.AddInheritanceClassToModelClass(parentClass);
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        var serializeFunction = root.FindChildByName<CodeFunction>($"Serialize{parentClass.Name.ToFirstCharacterUpperCase()}");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.Contains($"serialize{inheritedClass.Name.ToFirstCharacterUpperCase()}(writer, parentClass)", result);
    }
    [Fact]
    public async Task WritesSerializerBody()
    {
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
        Assert.Contains("writeEnumValue", result);
        Assert.Contains($"writer.writeAdditionalData", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DoesntWriteReadOnlyPropertiesInSerializerBody()
    {
        var model = root.AddClass(TestHelper.CreateModelClass("TestModel")).First();
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
        var errorClass = TestHelper.CreateModelClass("Error4XX");
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
        subNS.AddClass(errorClass);
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
}
