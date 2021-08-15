using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners
{
    public class PhpReservedNamesProvider: IReservedNamesProvider
    {
        private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
            "abstract",
            "and",
            "as",
            "break",
            "callable",
            "case",
            "catch",
            "class",
            "clone",
            "const",
            "continue",
            "declare",
            "default",
            "do",
            "echo",
            "else",
            "elseif",
            "empty",
            "enddeclare",
            "endfor",
            "endforeach",
            "endif",
            "endswitch",
            "extends",
            "final",
            "finally",
            "fn",
            "for",
            "foreach",
            "function",
            "global",
            "if",
            "implements",
            "include",
            "include_once",
            "instanceof",
            "insteadof",
            "interface",
            "isset",
            "list",
            "namespace",
            "new",
            "or",
            "print",
            "private",
            "protected",
            "public",
            "require",
            "require_once",
            "return",
            "static",
            "switch",
            "throw",
            "trait",
            "try",
            "use",
            "var",
            "while",
            "xor",
            "yield",
            "yield from"
        });

        public HashSet<string> ReservedNames => _reservedNames.Value;
    }
}
