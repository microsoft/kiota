using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Refiners;
public class CSharpReservedTypesProvider : IReservedNamesProvider
{
    private static IEnumerable<string> GetSystemTypeNames()
    {
        return typeof(string).Assembly.GetTypes()
                                .Where(static type => type.Namespace == "System"
                                                      && type.IsPublic // get public(we can only import public type in external code)
                                                      && !type.IsGenericType)// non generic types(generic type names have special character like `)
                                .Select(static type => type.Name);
    }

    private static readonly IEnumerable<string> CustomDefinedValues = new string[] {
        "file", //system.io static types
        "directory",
        "path",
        "environment",
        "task",
        "thread",
        "integer"
    };

    private readonly Lazy<HashSet<string>> _reservedNames = new(static () =>
    {
        return CustomDefinedValues.Union(GetSystemTypeNames()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
