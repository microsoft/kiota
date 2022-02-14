using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners {
    public class PythonReservedNamesProvider : IReservedNamesProvider {
        private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
            "and",
            "as",
            "assert",
            "break",
            "class",
            "continue",
            "def",
            "del",
            "elif",
            "else",
            "except",
            "finally",
            "false",
            "for",
            "from",
            "global",
            "if",
            "import",
            "in",
            "is",
            "lambda",
            "nonlocal",
            "None",
            "not",
            "or",
            "pass",
            "raise",
            "return",
            "True",
            "try",
            "with",
            "while",
            "yield",      
        });
        public HashSet<string> ReservedNames => _reservedNames.Value;
    }
}
