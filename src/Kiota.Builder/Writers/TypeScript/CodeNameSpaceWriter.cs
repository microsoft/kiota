using System;
using System.Collections.Generic;
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

        private static List<string> SortClasses(List<CodeClass> classes)
        {
            var visited = new HashSet<string>();
            var orderedList = new List<string>();

            foreach (var @class in classes)
            {


                if (!visited.Contains(@class.Name))
                {

                    visited.Add(@class.Name);
                    var usings = @class.StartBlock as CodeClass.Declaration;
                    var inheritsFrom = usings?.Inherits;

                    //  var declaration = usings.
                    if (inheritsFrom != null)
                    {
                        if (!visited.Contains(inheritsFrom.Name))
                        {
                            visited.Add(inheritsFrom.Name);


                            orderedList.Insert(0, inheritsFrom.Name);

                        }

                    }
                    orderedList.Add(@class.Name);
                }
            }
                return orderedList;
                // var usings = @class.StartBlock.Usings;

                //  usings.Select(us => {us.Name })
            
        }
    }
}
