using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript
{
    public class CodeNameSpaceWriter : BaseElementWriter<CodeNamespace, TypeScriptConventionService>
    {
        public CodeNameSpaceWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
        
        /// <summary>
        /// Writes export statements for classes and enums belonging to a namespace into a generated index.ts file. 
        /// The classes should be export in the order of inheritance so as to avoid circular dependency issues in javascript.
        /// </summary>
        /// <param name="codeElement">Code element is a code namespace</param>
        /// <param name="writer"></param>
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            var sortedClassNames = SortClassesInOrderOfInheritance(codeElement.Classes.ToList());

            foreach (var className in sortedClassNames)
            {
                writer.WriteLine($"export * from './{className.ToFirstCharacterLowerCase()}'");
            }
        }

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

                foreach (var child in parentChildrenMap[current])
                {
                    if (parentChildrenMap.ContainsKey(child))
                    {
                        VisitEveryChild(parentChildrenMap, visited, inheritanceOrderList, child);

                    }
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
            foreach (var @class in classes.Where(c => c.IsOfKind(CodeClassKind.Model)))
            {
                // Verify if parent class is from the same namespace
                var inheritsFrom = @class.StartBlock?.Inherits?.TypeDefinition?.Parent?.Name == @class.Parent.Name ? @class.StartBlock?.Inherits?.Name : null;

                if (inheritsFrom != null)
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
             * 3. Print all remaining classes which have not been visted or those do not have any parent/child relationship.
             */
            foreach (var @class in classes.Where(c => c.IsOfKind(CodeClassKind.Model) && !visited.Contains(c.Name) ))
            {
                if (!visited.Contains(@class.Name))
                {
                    visited.Add(@class.Name);
                    inheritanceOrderList.Add(@class.Name);
                }
            }
            return inheritanceOrderList;
        }
    }
}
