using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class RustReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase) {
        // Strict keywords
        "as", "break", "const", "continue", "crate", "else", "enum", "extern",
        "false", "fn", "for", "if", "impl", "in", "let", "loop", "match",
        "mod", "move", "mut", "pub", "ref", "return", "self", "Self",
        "static", "struct", "super", "trait", "true", "type", "unsafe",
        "use", "where", "while",
        // Async/await
        "async", "await", "dyn",
        // Reserved for future use
        "abstract", "become", "box", "do", "final", "macro", "override",
        "priv", "try", "typeof", "unsized", "virtual", "yield",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
