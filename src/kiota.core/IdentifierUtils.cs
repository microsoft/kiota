using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kiota.core
{
    public static class IdentifierUtils
    {
        public static string FirstUpperCase(string input)
        {
            if (input.Length == 0) return input;
            return Char.ToUpper(input[0]) + input.Substring(1);
        }

        public static string ToCamelCase(string name)
        {
            var chunks = name.Split("-");
            var identifier = String.Join(null, chunks.Take(1)
                                                  .Union(chunks.Skip(1)
                                                                .Select(s => FirstUpperCase(s))));
            return identifier;
        }
        public static string ToPascalCase(string name)
        {
            var chunks = name.Split("-");
            var identifier = String.Join(null, chunks.Select(s => FirstUpperCase(s)));
            return identifier;
        }
    }
}
