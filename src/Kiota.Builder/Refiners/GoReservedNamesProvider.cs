using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners {
    public class GoReservedNamesProvider : IReservedNamesProvider
    {
        private readonly Lazy<HashSet<string>> _reservedNames = new(() => new HashSet<string> {
            "break",
            "case",
            "chan",
            "const",
            "continue",
            "default",
            "defer",
            "else",
            "fallthrough",
            "for",
            "func",
            "go",
            "goto",
            "if",
            "import",
            "interface",
            "map",
            "package",
            "range",
            "return",
            "select",
            "struct",
            "switch",
            "type",
            "var"
        });
        public HashSet<string> ReservedNames => _reservedNames.Value;
    }
}
