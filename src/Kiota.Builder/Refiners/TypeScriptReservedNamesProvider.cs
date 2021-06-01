using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners {
    public class TypeScriptReservedNamesProvider : IReservedNamesProvider {
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
            "export",
            "extends",
            "false",
            "finally",
            "for",
            "function",
            "If",
            "import",
            "in",
            "istanceOf",
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
        });
        public HashSet<string> ReservedNames => _reservedNames.Value;
    }
}
