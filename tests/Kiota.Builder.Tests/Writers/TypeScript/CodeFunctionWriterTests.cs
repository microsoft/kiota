using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests;
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
    public void WritesModelFactoryBody() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
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
        factoryMethod.DiscriminatorMappings.TryAdd("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
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
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.Contains("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.Contains("if (mappingValueNode) {", result);
        Assert.Contains("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.Contains("if (mappingValue) {", result);
        Assert.Contains("switch (mappingValue) {", result);
        Assert.Contains("case \"ns.childmodel\":", result);
        Assert.Contains("return new ChildModel();", result);
        Assert.Contains("return new ParentModel();", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnMissingParameter() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
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
        factoryMethod.DiscriminatorMappings.TryAdd("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
        var factoryFunction = root.AddFunction(new CodeFunction(factoryMethod)).First();
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return new ChildModel();", result);
        Assert.Contains("return new ParentModel();", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyPropertyName() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var childModel = root.AddClass(new CodeClass {
            Name = "childModel",
            Kind = CodeClassKind.Model,
        }).First();
        (childModel.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
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
        factoryMethod.DiscriminatorMappings.TryAdd("ns.childmodel", new CodeType {
                        Name = "childModel",
                        TypeDefinition = childModel,
                    });
        factoryMethod.DiscriminatorPropertyName = string.Empty;
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
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return new ChildModel();", result);
        Assert.Contains("return new ParentModel();", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
    [Fact]
    public void DoesntWriteFactorySwitchOnEmptyMappings() {
        var parentModel = root.AddClass(new CodeClass {
            Name = "parentModel",
            Kind = CodeClassKind.Model,
        }).First();
        var factoryMethod = parentModel.AddMethod(new CodeMethod {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            },
            IsStatic = true,
        }).First();
        factoryMethod.DiscriminatorPropertyName = "@odata.type";
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
        writer.Write(factoryFunction);
        var result = tw.ToString();
        Assert.DoesNotContain("const mappingValueNode = parseNode.getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if (mappingValueNode) {", result);
        Assert.DoesNotContain("const mappingValue = mappingValueNode.getStringValue()", result);
        Assert.DoesNotContain("if (mappingValue) {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\":", result);
        Assert.DoesNotContain("return new ChildModel();", result);
        Assert.Contains("return new ParentModel();", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
}
