using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners
{
    public static class CrystalReservedNamesProvider
    {
        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            // Crystal keywords
            "abstract", "alias", "as", "asm", "begin", "break", "case", "class", 
            "def", "do", "else", "end", "ensure", "enum", "extend", "false", 
            "for", "fun", "if", "in", "include", "instance_sizeof", "module", 
            "next", "nil", "out", "pointerof", "private", "protected", "require", 
            "rescue", "return", "self", "sizeof", "struct", "super", "then", 
            "true", "type", "typeof", "union", "unless", "until", "verbatim", 
            "when", "while", "with", "yield"
        };

        public static bool IsReserved(string name)
        {
            return ReservedNames.Contains(name);
        }
    }
}
