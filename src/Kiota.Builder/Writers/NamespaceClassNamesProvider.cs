using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace  Kiota.Builder.Writers;

public static class NamespaceClassNamesProvider {
    /// <summary>
    /// Orders given list of classes in a namespace based on inheritance.
    /// That is, if class B extends class A then A should exported before class B.
    /// </summary>
    /// <param name="codeNamespace"> Code Namespace to get the classes for</param>
    /// <returns> List of class names in the code name space ordered based on inheritance</returns>
    public static List<string> SortClassesInOrderOfInheritance(CodeNamespace codeNamespace)
    {
        return codeNamespace.Classes.Where(c => c.IsOfKind(CodeClassKind.Model))
                                                .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase) //ordering is important to get a deterministic output
                                                .Select(static x => x.GetInheritanceTree(true))
                                                .SelectMany(static x => x)
                                                .Select(static x => x.Name)
                                                .Concat(codeNamespace.Classes.Where(c => c.IsOfKind(CodeClassKind.Model) && c.StartBlock.Inherits is null)
                                                    .Select(static x => x.Name)
                                                    .Order(StringComparer.OrdinalIgnoreCase)) //ordering is important to get a deterministic output
                                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                                .ToList();

    }
}
