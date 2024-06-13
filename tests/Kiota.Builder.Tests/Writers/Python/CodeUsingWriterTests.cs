using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Python;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public class CodeUsingWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly LanguageWriter writer;
    private readonly StringWriter tw;
    private readonly CodeNamespace root;
    public CodeUsingWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
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
            Name = "Bar",
            Alias = "baz",
            Declaration = new CodeType
            {
                Name = "Bar",
                TypeDefinition = codeClass,
            },
        };
        codeClass.AddUsing(us);
        usingWriter.WriteInternalImports(codeClass, writer);
        var result = tw.ToString();
        Assert.Contains("from .bar import Bar as baz", result);
    }
    [Fact]
    public void WritesRefinedNames()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "BarBaz",
        }).First();
        var us = new CodeUsing
        {
            Name = "BarBaz",
            Declaration = new CodeType
            {
                Name = "BarBaz",
                TypeDefinition = codeClass,
            },
        };
        codeClass.AddUsing(us);
        usingWriter.WriteInternalImports(codeClass, writer);
        var result = tw.ToString();
        Assert.Contains("from .bar_baz import BarBaz", result);
    }
    [Fact]
    public void DoesntAliasRegularSymbols()
    {
        var usingWriter = new CodeUsingWriter("foo");
        var codeClass = root.AddClass(new CodeClass
        {
            Name = "Bar",

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
        codeClass.AddUsing(us);
        usingWriter.WriteInternalImports(codeClass, writer);
        var result = tw.ToString();
        Assert.Contains("from .bar import Bar", result);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("it-IT")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("pt-BR")]
    [InlineData("ru-RU")]
    [InlineData("zh-CN")]
    [InlineData("zh-TW")]
    public void FutureShouldBeSortedBeforeDatetimeInDifferentCultures(string cultureName)
    {
        var cultureInfo = new CultureInfo(cultureName);
        var compareInfo = cultureInfo.CompareInfo;

        var sorted = new string[] { null, "__future__" }.OrderBy(x => x, StringComparer.Create(cultureInfo, CompareOptions.IgnoreCase)).ToArray();

        // Half pass, half fail
        // I know in the CodeUsingWriter - We are grouping on x.Declaration?.Name - Which can be null?
        Assert.True(sorted[0] == "__future__");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("it-IT")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("pt-BR")]
    [InlineData("ru-RU")]
    [InlineData("zh-CN")]
    [InlineData("zh-TW")]
    public void WritesFutureImportsFirst(string cultureName)
    {
        // Generated with Kiota mcr.microsoft.com/openapi/kiota:1.15.0

        // import datetime
        // from __future__ import annotations
        // from dataclasses import dataclass, field
        // from kiota_abstractions.serialization import AdditionalDataHolder, Parsable, ParseNode, SerializationWriter
        // from typing import Any, Callable, Dict, List, Optional, TYPE_CHECKING, Union

        Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);

        var writer2 = LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName);
        var tw2 = new StringWriter();
        writer2.SetTextWriter(tw2);
        var root2 = CodeNamespace.InitRootNamespace();

        var usingWriter = new CodeUsingWriter("foo");

        var codeClass = new ClassDeclaration
        {
            Name = "Test",
        };

        codeClass.AddUsings(new CodeUsing { Name = "annotations", Declaration = new CodeType { Name = "__future__", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "dataclass", Declaration = new CodeType { Name = "dataclasses", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "field", Declaration = new CodeType { Name = "dataclasses", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "AdditionalDataHolder", Declaration = new CodeType { Name = "kiota_abstractions.serialization", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Parsable", Declaration = new CodeType { Name = "kiota_abstractions.serialization", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "ParseNode", Declaration = new CodeType { Name = "kiota_abstractions.serialization", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "SerializationWriter", Declaration = new CodeType { Name = "kiota_abstractions.serialization", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Any", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Callable", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Dict", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "List", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Optional", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "TYPE_CHECKING", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "Union", Declaration = new CodeType { Name = "typing", IsExternal = true } });
        codeClass.AddUsings(new CodeUsing { Name = "datetime", Declaration = new CodeType { Name = "-", IsExternal = true } });

        usingWriter.WriteExternalImports(codeClass, writer2);

        string[] usings = tw2.ToString().Split(tw2.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("from __future__ import annotations", usings.First());
        Assert.Equal("import datetime", usings[1]);
        Assert.Equal("from dataclasses import dataclass, field", usings[2]);
        Assert.Equal("from kiota_abstractions.serialization import AdditionalDataHolder, Parsable, ParseNode, SerializationWriter", usings[3]);
        Assert.Equal("from typing import Any, Callable, Dict, List, Optional, TYPE_CHECKING, Union", usings[4]);
    }
}
