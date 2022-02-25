using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.TypeScript
{
    public class CodeNameSpaceWriter : BaseElementWriter<CodeNamespace, TypeScriptConventionService>
    {

        public CodeNameSpaceWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
        public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
        {
            var classes = SortClasses(codeElement.Classes.ToList());

            foreach (var c in classes)
            {


                writer.WriteLine($"export * from './{c.ToFirstCharacterUpperCase()}'");

            }


            var enums = codeElement.Enum;
            if (enums != null && enums.Any())
            {
                foreach (var e in enums)
                {

                    writer.WriteLine($"export * from './{e.Name.ToFirstCharacterUpperCase()}'");

                }
            }
        }

        private void visitChild(Dictionary<string, List<string>> parentListChildren, HashSet<string> visited, List<string> orderedList, string current)
        {

            if (!visited.Contains(current))
            {
               

               visited.Add(current);

                    foreach (var child in parentListChildren[current])
                {  if (parentListChildren.ContainsKey(child))
                    {
                        visitChild(parentListChildren, visited, orderedList, child);

                    }
                }
                orderedList.Insert(0,current);
            }
                




            
        }
        private  List<string> SortClasses(List<CodeClass> classes)
        {
            var visited = new HashSet<string>();
            var childParent = new Dictionary<string, string>();
            var parentListChildren = new Dictionary<string, List<string>>();
            var orderedList = new List<string>();



            foreach (var @class in classes)
            {
                var usings = @class.StartBlock as CodeClass.Declaration;
                var inheritsFrom = usings?.Inherits?.Name;
                if (inheritsFrom != null)    
                {
                    childParent.Add(@class.Name, inheritsFrom);

                    if (!parentListChildren.ContainsKey(inheritsFrom))
                    {
                        parentListChildren[inheritsFrom] = new List<string>();
                    }
                    parentListChildren[inheritsFrom].Add(@class.Name);

                }
            }



            foreach (var key in parentListChildren.Keys)
            {

                visitChild(parentListChildren, visited, orderedList, key);
            }

            foreach (var @class in classes) {
              
                if (!visited.Contains(@class.Name))
                {
                    visited.Add(@class.Name);
                    orderedList.Add(@class.Name);

                }
               

            }
         

            //foreach (var @class in classes)
            //{

            //    var usings = @class.StartBlock as CodeClass.Declaration;
            //    var inheritsFrom = usings?.Inherits;
            //    if (!visited.Contains(@class.Name))
            //    {

            //        visited.Add(@class.Name);



            //        //  var declaration = usings.
            //        if (inheritsFrom != null)
            //        {
            //            if (!visited.Contains(inheritsFrom.Name))
            //            {
            //                visited.Add(inheritsFrom.Name);


            //                orderedList.Insert(0, inheritsFrom.Name);

            //            }

            //        }
            //        orderedList.Add(@class.Name);
            //    }


            //}
            return orderedList;

        }
    }
}
