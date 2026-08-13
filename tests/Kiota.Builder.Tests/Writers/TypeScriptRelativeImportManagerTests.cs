using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class TypeScriptRelativeImportManagerTests
{
    private readonly CodeNamespace root;
    private readonly CodeNamespace graphNS;
    private readonly CodeClass parentClass;
    private readonly TypescriptRelativeImportManager importManager = new("graph", '.');
    public TypeScriptRelativeImportManagerTests()
    {
        root = CodeNamespace.InitRootNamespace();
        graphNS = root.AddNamespace("graph");
        parentClass = new()
        {
            Name = "parentClass"
        };
        graphNS.AddClass(parentClass);
    }
    [Fact]
    public void ReplacesImportsSubNamespace()
    {
        var rootNS = parentClass.Parent as CodeNamespace;
        rootNS.RemoveChildElement(parentClass);
        graphNS.AddClass(parentClass);
        var declaration = parentClass.StartBlock;
        var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = messageClassDef.Name,
            Declaration = new()
            {
                Name = messageClassDef.Name,
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        var result = importManager.GetRelativeImportPathForUsing(nUsing, graphNS);
        Assert.Equal("./messages/index.js", result.Item3);
    }
    [Fact]
    public void ReplacesImportsParentNamespace()
    {
        var declaration = parentClass.StartBlock;
        var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");
        graphNS.RemoveChildElement(parentClass);
        modelsNS.AddClass(parentClass);
        var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        subNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = messageClassDef.Name,
            Declaration = new()
            {
                Name = messageClassDef.Name,
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        var result = importManager.GetRelativeImportPathForUsing(nUsing, modelsNS);
        Assert.Equal("../messages/index.js", result.Item3);
    }
    [Fact]
    public void ReplacesImportsInOtherTrunk()
    {
        var usedRangeNS1 = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook.tables.worksheet.pivotTables.usedRange");
        var usedRangeNS2 = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook.worksheets.usedRange");
        var workbookNS = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook");
        var workbookRangeClassDef = new CodeClass
        {
            Name = "workbookRange",
        };
        workbookNS.AddClass(workbookRangeClassDef);
        var usedRangeClassDef1 = new CodeClass
        {
            Name = "usedRangeRequestBuilder",
        };
        usedRangeNS1.AddClass(usedRangeClassDef1);

        var declaration1 = usedRangeClassDef1.StartBlock;
        var nUsing = new CodeUsing
        {
            Name = workbookNS.Name,
            Declaration = new()
            {
                Name = workbookRangeClassDef.Name,
                TypeDefinition = workbookRangeClassDef,
            }
        };
        declaration1.AddUsings(nUsing);
        var usedRangeClassDef2 = new CodeClass
        {
            Name = "usedRangeRequestBuilder",
        };
        usedRangeNS2.AddClass(usedRangeClassDef2);
        var declaration2 = usedRangeClassDef2.StartBlock;
        var nUsing2 = new CodeUsing
        {
            Name = workbookNS.Name,
            Declaration = new()
            {
                Name = workbookRangeClassDef.Name,
                TypeDefinition = workbookRangeClassDef,
            }
        };
        declaration2.AddUsings(nUsing2);
        var result = importManager.GetRelativeImportPathForUsing(nUsing, usedRangeNS1);
        var result2 = importManager.GetRelativeImportPathForUsing(nUsing2, usedRangeNS2);
        Assert.Equal("../../../../index.js", result.Item3);
        Assert.Equal("../../index.js", result2.Item3);
    }
    [Fact]
    public void ReplacesImportsSameNamespace()
    {
        var declaration = parentClass.StartBlock;
        var messageClassDef = new CodeClass
        {
            Name = "Message",
        };
        graphNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = "graph",
            Declaration = new()
            {
                Name = "Message",
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        var result = importManager.GetRelativeImportPathForUsing(nUsing, graphNS);
        Assert.Equal("./index.js", result.Item3);
    }

    [Fact]
    public void ReplacesImportsSameNamespaceIndex()
    {
        var declaration = parentClass.StartBlock;
        var messageClassDef = new CodeClass
        {
            Name = "Message",
            Kind = CodeClassKind.Model
        };
        graphNS.AddClass(messageClassDef);
        var nUsing = new CodeUsing
        {
            Name = "graph",
            Declaration = new()
            {
                Name = "Message",
                TypeDefinition = messageClassDef,
            }
        };
        declaration.AddUsings(nUsing);
        var result = importManager.GetRelativeImportPathForUsing(nUsing, graphNS);
        Assert.Equal("./index.js", result.Item3);
    }
}
