using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace  Kiota.Builder.Writers;

public static class NamespaceClassNamesProvider {
    /// <summary>
    /// Visits every child for a given parent class and recursively inserts each class into a list ordered based on inheritance.
    /// </summary>
    /// <param name="parentListChildren"></param>
    /// <param name="visited"></param>
    /// <param name="orderedList"> Lis</param>
    /// <param name="current"></param>
    private static void VisitEveryChild(Dictionary<string, List<string>> parentChildrenMap, HashSet<string> visited, List<string> inheritanceOrderList, string current)
    {
        if (!visited.Contains(current) && parentChildrenMap.TryGetValue(current, out var children))
        {
            visited.Add(current);

            foreach (var child in children.Where(parentChildrenMap.ContainsKey)
                                        .Order(StringComparer.OrdinalIgnoreCase)) //ordering is important to get a deterministic output
            {
                VisitEveryChild(parentChildrenMap, visited, inheritanceOrderList, child);
            }
            inheritanceOrderList.Insert(0, current);
        }
    }
    /// <summary>
    /// Orders given list of classes in a namespace based on inheritance.
    /// That is, if class B extends class A then A should exported before class B.
    /// </summary>
    /// <param name="classes"> Classes in a given code namespace</param>
    /// <returns> List of class names in the code name space ordered based on inheritance</returns>
    public static List<string> SortClassesInOrderOfInheritance(IEnumerable<CodeClass> classes)
    {
        var visited = new HashSet<string>();
        var parentChildrenMap = new Dictionary<string, List<string>>();
        var inheritanceOrderList = new List<string>();

        /*
        * 1. Create a dictionary containing all the parent classes.
        */
        foreach (var @class in classes.Where(static c => c.IsOfKind(CodeClassKind.Model) &&
                                                c.StartBlock.Inherits?.TypeDefinition is CodeClass definitionClass &&
                                                c.GetImmediateParentOfType<CodeNamespace>() == definitionClass.GetImmediateParentOfType<CodeNamespace>()))
                                                // Verify if parent class is from the same namespace
        {
            if (parentChildrenMap.TryGetValue(@class.StartBlock.Inherits.Name, out var children))
                children.Add(@class.Name);
            else
                parentChildrenMap.Add(@class.StartBlock.Inherits.Name, new() { @class.Name });
        }

        /*
        * 2. Print the export command for every parent node before the child node.
        */
        foreach (var key in parentChildrenMap.Keys.Order(StringComparer.OrdinalIgnoreCase)) //ordering is important to get a deterministic output
        {
            VisitEveryChild(parentChildrenMap, visited, inheritanceOrderList, key);
        }

        /*
        * 3. Print all remaining classes which have not been visited or those do not have any parent/child relationship.
        */
        inheritanceOrderList.AddRange(classes.Where(c => c.IsOfKind(CodeClassKind.Model) && !visited.Contains(c.Name))
                                    .Select(static x => x.Name)
                                    .Order(StringComparer.OrdinalIgnoreCase)); //ordering is important to get a deterministic output
        return inheritanceOrderList;
    }
}
