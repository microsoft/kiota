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
    public static void WriteClassesInOrderOfInheritance(CodeNamespace codeNamespace, Action<CodeClass> callbackToWriteImport)
    {
        var writtenClassNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inheritanceBranches = codeNamespace.Classes.Where(c => c.IsOfKind(CodeClassKind.Model))
                                                .Select(static x => x.GetInheritanceTree(true))
                                                .ToList();
        var maxDepth = inheritanceBranches.Any() ? inheritanceBranches.Max(static x => x.Count) : 0;
        for(var depth = 0; depth < maxDepth; depth++)
            foreach(var name in inheritanceBranches
                                                .Where(x => x.Count > depth)
                                                .Select(x => x[depth])
                                                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)//order is important to get a deterministic output
                                                .Where(x => writtenClassNames.Add(x.Name))) // linq distinct does not guarantee order
                    callbackToWriteImport(name);
    }
}
