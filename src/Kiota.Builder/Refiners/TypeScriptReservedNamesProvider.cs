﻿using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;
public class TypeScriptReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
        "break",
        "case",
        "catch",
        "class",
        "const",
        "continue",
        "debugger",
        "default",
        "do",
        "else",
        "enum",
        "error",
        "export",
        "extends",
        "false",
        "finally",
        "for",
        "function",
        "If",
        "import",
        "in",
        "instanceOf",
        "new",
        "null",
        "return",
        "super",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeOf",
        "var",
        "void",
        "while",
        "with",
        "BaseRequestBuilder",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
