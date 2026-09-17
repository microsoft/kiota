using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class RustReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.Ordinal) {
        // Strict keywords
        "as",
        "async",
        "await",
        "break",
        "const",
        "continue",
        "crate",
        "dyn",
        "else",
        "enum",
        "extern",
        "false",
        "fn",
        "for",
        "if",
        "impl",
        "in",
        "let",
        "loop",
        "match",
        "mod",
        "move",
        "mut",
        "pub",
        "ref",
        "return",
        "self",
        "Self",
        "static",
        "struct",
        "super",
        "trait",
        "true",
        "type",
        "unsafe",
        "use",
        "where",
        "while",
        // Reserved for future use
        "abstract",
        "become",
        "box",
        "do",
        "final",
        "macro",
        "override",
        "priv",
        "try",
        "typeof",
        "unsized",
        "virtual",
        "yield",
        // Weak keywords used in certain contexts
        "union",
        "dyn",
        // Common standard library types/traits that could collide
        "String",
        "Vec",
        "Box",
        "Option",
        "Result",
        "HashMap",
        "Clone",
        "Default",
        "Display",
        "Debug",
        "Iterator",
        "From",
        "Into",
        "Error",
        // Kiota base type names
        "BaseRequestBuilder",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
