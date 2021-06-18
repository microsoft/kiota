using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, TypeScriptConventionService>
    {
        public CodeClassDeclarationWriter(TypeScriptConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            foreach (var codeUsing in codeElement.Usings
                                        .GroupBy(x => x.Declaration?.Name)
                                        .OrderBy(x => x.Key))
                writer.WriteLine($"import {{{codeUsing.Select(x => x.Name).Distinct().Aggregate((x,y) => x + ", " + y)}}} from '{codeUsing.Key}';");
            writer.WriteLine();
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
    }
}
