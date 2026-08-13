using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public class RubyReservedNamesProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase) {
        "BEGIN",
        "END",
        "alias",
        "and",
        "begin",
        "break",
        "case",
        "class",
        "def",
        "module",
        "next",
        "nil",
        "not",
        "or",
        "redo",
        "rescue",
        "retry",
        "return",
        "elsif",
        "end",
        "false",
        "ensure",
        "for",
        "if",
        "true",
        "undef",
        "unless",
        "do",
        "else",
        "super",
        "then",
        "until",
        "when",
        "while",
        "defined?",
        "self",
        "BaseRequestBuilder",
        "ObjectId",
        "object_id",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
