using System;
using System.IO;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Dart;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public sealed class CodeClassDeclarationWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private const string DefaultNameSpace = "ns";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeClassDeclarationWriter codeElementWriter;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;

    public CodeClassDeclarationWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, DefaultPath, DefaultName);
        codeElementWriter = new CodeClassDeclarationWriter(new DartConventionService(), DefaultNameSpace, (Builder.PathSegmenters.DartPathSegmenter)writer.PathSegmenter);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new()
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesSimpleDeclaration()
    {
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("class", result);
    }
    [Fact]
    public void WritesImplementation()
    {
        var declaration = parentClass.StartBlock;
        declaration.AddImplements(new CodeType
        {
            Name = "someInterface"
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("implements someInterface", result);
    }
    [Fact]
    public void WritesInheritance()
    {
        var declaration = parentClass.StartBlock;
        declaration.Inherits = new()
        {
            Name = "someParent"
        };
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("extends", result);
        Assert.Contains("SomeParent", result);
    }
    [Fact]
    public void WritesImports()
    {
        var declaration = parentClass.StartBlock;
        CodeClass messageClass = new()
        {
            Name = "Message"
        };
        root.AddClass(messageClass);
        declaration.AddUsings(new CodeUsing()
        {
            Name = "project.graph",
            Declaration = new()
            {
                Name = "Message",
                TypeDefinition = messageClass
            }
        });
        codeElementWriter.WriteCodeElement(declaration, writer);
        var result = tw.ToString();
        Assert.Contains("import './message.dart';", result);
    }
    [Fact]
    public void WritesGenericClassDeclaration()
    {
        parentClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "TItemType" });
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("class parentClass<TItemType extends Parsable>", result);
    }
    [Fact]
    public void WritesGenericDerivedConstructorForwardingToGenericBase()
    {
        var itemTypeParameter = new CodeTypeParameter { Name = "TItemType" };
        parentClass.StartBlock.AddTypeParameter(itemTypeParameter);
        var baseClass = new CodeClass { Name = "BasePage" };
        baseClass.StartBlock.AddTypeParameter(new CodeTypeParameter { Name = "TItemType" }); // the base owns its parameter instance
        var inherits = new CodeType { TypeDefinition = baseClass };
        inherits.AddGenericTypeParameterValue(new CodeType { TypeDefinition = parentClass.TypeParameters[0] }); // derived closes over its own parameter
        parentClass.StartBlock.Inherits = inherits;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "page",
            Type = new CodeType { Name = "int", IsExternal = true },
        });
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("class parentClass<TItemType extends Parsable> extends BasePage<TItemType>", result);
        Assert.Contains("parentClass(ParsableFactory<TItemType> itemTypeFactory) : super(itemTypeFactory);", result);
        // the derived class deserializes no TItemType property of its own, the base owns the factory field
        Assert.DoesNotContain("_itemTypeFactory", result);
    }
    [Fact]
    public void WritesFactoryFieldOnlyForOwnDeserializers()
    {
        var itemTypeParameter = new CodeTypeParameter { Name = "TItemType" };
        parentClass.StartBlock.AddTypeParameter(itemTypeParameter);
        var itemsType = new CodeType { TypeDefinition = itemTypeParameter, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex };
        parentClass.AddProperty(new CodeProperty
        {
            Name = "items",
            Type = itemsType,
        });
        codeElementWriter.WriteCodeElement(parentClass.StartBlock, writer);
        var result = tw.ToString();
        Assert.Contains("final ParsableFactory<TItemType> _itemTypeFactory;", result);
        Assert.Contains("parentClass(ParsableFactory<TItemType> itemTypeFactory) : _itemTypeFactory = itemTypeFactory;", result);
    }
}
