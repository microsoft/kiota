using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, JavaConventionService>
    {
        public CodeClassDeclarationWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            if(codeElement?.Parent?.Parent is CodeNamespace ns) {
                writer.WriteLine($"package {ns.Name};");
                writer.WriteLine();
                codeElement.Usings
                    .Where(x => x.Declaration != null)
                    .Where(x => x.Declaration.IsExternal || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(x => x.Declaration.IsExternal ?
                                     $"import {x.Declaration.Name}.{x.Name.ToFirstCharacterUpperCase()};" :
                                     $"import {x.Name}.{x.Declaration.Name.ToFirstCharacterUpperCase()};")
                    .Distinct()
                    .GroupBy(x => x.Split('.').Last())
                    .Where(x => x.Count() == 1) // we don't want to import the same symbol twice
                    .SelectMany(x => x)
                    .OrderBy(x => x)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            }
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass)?.Description, writer);
            writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
    }

}
