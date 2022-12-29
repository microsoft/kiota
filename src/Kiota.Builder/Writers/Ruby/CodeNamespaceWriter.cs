using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby;
public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
    public CodeNamespaceWriter(RubyConventionService conventionService) : base(conventionService){}
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        var sortedClassNames = SortClassesInOrderOfInheritance(codeElement.Classes.ToList());
        foreach(var childModel in codeElement.GetChildElements(true).OfType<CodeEnum>())
            writer.WriteLine($"require_relative '{childModel.Name.ToSnakeCase()}'");
        foreach (var className in sortedClassNames)
            writer.WriteLine($"require_relative '{className.ToSnakeCase()}'");
        writer.StartBlock($"module {codeElement.Name.NormalizeNameSpaceName("::")}");
        writer.CloseBlock("end");
    }
    //TODO this comes from typescript, refactor

    /// <summary>
    /// Visits every child for a given parent class and recursively inserts each class into a list ordered based on inheritance.
    /// </summary>
    /// <param name="parentListChildren"></param>
    /// <param name="visited"></param>
    /// <param name="orderedList"> Lis</param>
    /// <param name="current"></param>
    private void VisitEveryChild(Dictionary<string, List<string>> parentChildrenMap, HashSet<string> visited, List<string> inheritanceOrderList, string current)
    {
        if (!visited.Contains(current))
        {
            visited.Add(current);

            foreach (var child in parentChildrenMap[current].Where(parentChildrenMap.ContainsKey))
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
    private List<string> SortClassesInOrderOfInheritance(List<CodeClass> classes)
    {
        var visited = new HashSet<string>();
        var parentChildrenMap = new Dictionary<string, List<string>>();
        var inheritanceOrderList = new List<string>();

        /*
            * 1. Create a dictionary containing all the parent classes.
            */
        foreach (var @class in classes.Where(static c => c.IsOfKind(CodeClassKind.Model)))
        {
            // Verify if parent class is from the same namespace
            var inheritsFrom = @class.Parent.Name.Equals(@class.StartBlock.Inherits?.TypeDefinition?.Parent?.Name, StringComparison.OrdinalIgnoreCase) ? @class.StartBlock.Inherits?.Name : null;

            if (!string.IsNullOrEmpty(inheritsFrom))
            {
                if (!parentChildrenMap.ContainsKey(inheritsFrom))
                {
                    parentChildrenMap[inheritsFrom] = new List<string>();
                }
                parentChildrenMap[inheritsFrom].Add(@class.Name);
            }
        }

        /*
            * 2. Print the export command for every parent node before the child node.
            */
        foreach (var key in parentChildrenMap.Keys)
        {
            VisitEveryChild(parentChildrenMap, visited, inheritanceOrderList, key);
        }

        /*
            * 3. Print all remaining classes which have not been visited or those do not have any parent/child relationship.
            */
        foreach (var className in classes.Where(c => c.IsOfKind(CodeClassKind.Model) && !visited.Contains(c.Name)).Select(static x => x.Name))
        {
                visited.Add(className);
                inheritanceOrderList.Add(className);
        }
        return inheritanceOrderList;
    }
}
