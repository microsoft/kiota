using System;
using System.IO;
using System.Linq;
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

    [Fact]
    public void WritesFutureImportsFirst()
    {
        var usingWriter = new CodeUsingWriter("foo");

        var cd = new ClassDeclaration
        {
            Name = "bar",
        };

        /* Add external imports */
        // import datetime
        // from __future__ import annotations
        // from dataclasses import dataclass, field
        // from kiota_abstractions.serialization import Parsable, ParseNode, SerializationWriter
        // from typing import Any, Callable, Dict, List, Optional, TYPE_CHECKING, Union

        cd.AddUsings(new CodeUsing
        {
            Name = "datetime",
            Declaration = new CodeType
            {
                Name = "-",
                IsExternal = true
            },

        });

        cd.AddUsings(new CodeUsing
        {
            Name = "annotations",
            Declaration = new CodeType
            {
                Name = "__future__",
                IsExternal = true
            },

        });

        cd.AddUsings(new CodeUsing
        {
            Name = "dataclass",
            Declaration = new CodeType
            {
                Name = "dataclasses",
                IsExternal = true
            }
        });

        cd.AddUsings(new CodeUsing
        {
            Name = "field",
            Declaration = new CodeType
            {
                Name = "dataclasses",
                IsExternal = true
            }
        });

        cd.AddUsings(new CodeUsing[]
        {
            new CodeUsing
            {
                Name = "Parsable",
                Declaration = new CodeType
                {
                    Name = "kiota_abstractions.serialization",
                    IsExternal = true
                }
            }, new CodeUsing
            {
                Name = "ParseNode",
                Declaration = new CodeType
                {
                    Name = "kiota_abstractions.serialization",
                    IsExternal = true
                }
            },
            new CodeUsing
            {
                Name = "SerializationWriter",
                Declaration = new CodeType
                {
                    Name = "kiota_abstractions.serialization",
                    IsExternal = true
                }
            }}
        );

        usingWriter.WriteExternalImports(cd, writer);
        var result = tw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("from __future__ import annotations", result[0]);
        Assert.Equal("import datetime", result[1]);
    }
}
