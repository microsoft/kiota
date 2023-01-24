using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;
public class CodeFunctionWriterTests : IDisposable {
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeMethod method;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";

    private void AddInheritanceClass()
    {
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "someParentClass",
        };
    }
    private void AddSerializationProperties()
    {
        var addData = parentClass.AddProperty(new CodeProperty
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
        }).First();
        addData.Type = new CodeType
        {
            Name = "string"
        };
        var dummyProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
        }).First();
        dummyProp.Type = new CodeType
        {
            Name = "string"
        };
        var dummyCollectionProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyColl",
        }).First();
        dummyCollectionProp.Type = new CodeType
        {
            Name = "string",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
        };
        var dummyComplexCollection = parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyComplexColl"
        }).First();
        dummyComplexCollection.Type = new CodeType
        {
            Name = "Complex",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            TypeDefinition = new CodeClass
            {
                Name = "SomeComplexType"
            }
        };
        var dummyEnumProp = parentClass.AddProperty(new CodeProperty
        {
            Name = "dummyEnumCollection",
        }).First();
        dummyEnumProp.Type = new CodeType
        {
            Name = "SomeEnum",
            TypeDefinition = new CodeEnum
            {
                Name = "EnumType"
            }
        };
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            OriginalPropertyFromBaseType = new CodeProperty
            {
                Name = "definedInParent",
                Type = new CodeType
                {
                    Name = "string"
                }
            }
        });
    }
    public CodeFunctionWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        method = new CodeMethod {
            Name = MethodName,
        };
        method.ReturnType = new CodeType {
            Name = ReturnTypeName
        };
        parentClass.AddMethod(method);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WritesModelFactoryBody() {
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
        childModel.StartBlock.Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
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
    public async Task DoesntWriteFactorySwitchOnMissingParameter() {

        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
        childModel.StartBlock.Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
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
    public async Task DoesntWriteFactorySwitchOnEmptyPropertyName() {
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var childModel = root.AddClass(TestHelper.CreateModelClass("childModel")).First();
        childModel.StartBlock.Inherits = new CodeType {
            Name = "parentModel",
            TypeDefinition = parentModel,
        };
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.AddDiscriminatorMapping("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = string.Empty;
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
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
    public async Task DoesntWriteFactorySwitchOnEmptyMappings() {
        var parentModel = root.AddClass(TestHelper.CreateModelClass("parentModel")).First();
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode",
                TypeDefinition = new CodeClass {
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
    public void WritesInheritedDeSerializerBody()
    {
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("...super", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesDeSerializerBody()
    {
        method.Kind = CodeMethodKind.Deserializer;
        method.IsAsync = false;
        AddSerializationProperties();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("getStringValue", result);
        Assert.Contains("getCollectionOfPrimitiveValues", result);
        Assert.Contains("getCollectionOfObjectValues", result);
        Assert.Contains("getEnumValue", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void WritesInheritedSerializerBody()
    {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        AddInheritanceClass();
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("super.serialize", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public async Task WritesSerializerBody()
    {
        method.Kind = CodeMethodKind.Serializer;
        method.IsAsync = false;
        AddSerializationProperties();
        await ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
        writer.Write(method);
        var result = tw.ToString();
        Assert.Contains("writeStringValue", result);
        Assert.Contains("writeCollectionOfPrimitiveValues", result);
        Assert.Contains("writeCollectionOfObjectValues", result);
        Assert.Contains("writeEnumValue", result);
        Assert.Contains("writeAdditionalData(this.additionalData);", result);
        Assert.DoesNotContain("definedInParent", result, StringComparison.OrdinalIgnoreCase);
        AssertExtensions.CurlyBracesAreClosed(result);
    }

    [Fact]
    public async Task DoesntWriteReadOnlyPropertiesInSerializerBody()
    {
        var model = root.AddClass(TestHelper.CreateModelClass()).First();
        //method.Kind = CodeMethodKind.Serializer;
        //AddSerializationProperties();
        //AddInheritanceClass();
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
        var serializeFunction = root.FindChildByName<CodeFunction>("SerializeModel");
        writer.Write(serializeFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("readOnlyProperty", result);
        Assert.Contains("someProperty", result);
    }
}
