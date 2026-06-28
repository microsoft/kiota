using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public class CodeUsingWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly LanguageWriter writer;
    private readonly StringWriter tw;
    private readonly CodeNamespace root;
    public CodeUsingWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
    }
    [Fact]
    public void WritesAliasedSymbol()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Alias = "baz",
            Declaration = new CodeType
            {
                Name = "bar",
                TypeDefinition = codeClass,
            },
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { Bar as baz } from", result);
    }
    [Fact]
    public void DoesntAliasRegularSymbols()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = "bar",
                TypeDefinition = codeClass,
            },
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForGeneratedInterfaces()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var someInterface = new CodeInterface
        {
            Name = "Bar",
            Kind = CodeInterfaceKind.Model,
            OriginalClass = new CodeClass() { Name = "Bar" }
        };
        root.AddInterface(someInterface);
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = someInterface.Name,
                TypeDefinition = someInterface,
            },
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForDenotedExternalLibraries()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = codeClass.Name,
                TypeDefinition = codeClass,
            },
            IsErasable = true,
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForRequestConfiguration()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
            Kind = CodeClassKind.RequestConfiguration
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = codeClass.Name,
                TypeDefinition = codeClass,
            }
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForQueryParameters()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
            Kind = CodeClassKind.QueryParameters
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = codeClass.Name,
                TypeDefinition = codeClass,
            }
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForModel()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "bar",
            Kind = CodeClassKind.Model
        }).First();
        var us = new CodeUsing
        {
            Name = "bar",
            Declaration = new CodeType
            {
                Name = codeClass.Name,
                TypeDefinition = codeClass,
            }
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type Bar } from", result);
    }

    [Fact]
    public void WritesImportTypeStatementForEnum()
    {
        // Enums are generated as TypeScript type aliases (e.g., `export type MyEnum = ...`),
        // so they must use `import type` for compatibility with verbatimModuleSyntax.
        // See: https://github.com/microsoft/kiota/issues/2959
        var usingWriter = new CodeUsingWriter("foo");
        var codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "MyStatus",
        }).First();
        var us = new CodeUsing
        {
            Name = "MyStatus",
            Declaration = new CodeType
            {
                Name = codeEnum.Name,
                TypeDefinition = codeEnum,
            }
        };
        usingWriter.WriteCodeElement(new CodeUsing[] { us }, root, writer);
        var result = tw.ToString();
        Assert.Contains("// @ts-ignore", result);
        Assert.Contains("import { type MyStatus } from", result);
    }
}
