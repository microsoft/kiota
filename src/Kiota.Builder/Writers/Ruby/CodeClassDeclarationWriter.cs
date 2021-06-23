using System.Linq;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, RubyConventionService>
    {
        public CodeClassDeclarationWriter(RubyConventionService conventionService) : base(conventionService){}
        
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            foreach (var codeUsing in codeElement.Usings
                                        .Where(x => x.Declaration.IsExternal)
                                        .Distinct()
                                        .GroupBy(x => x.Declaration?.Name)
                                        .OrderBy(x => x.Key))
                writer.WriteLine($"require '{codeUsing.Key.ToSnakeCase()}'");
            foreach (var codeUsing in codeElement.Usings
                                        .Where(x => !x.Declaration.IsExternal)
                                        .Distinct()
                                        .GroupBy(x => x.Declaration?.Name)
                                        .OrderBy(x => x.Key))
                writer.WriteLine($"require_relative '{codeUsing.Key.ToSnakeCase()}'");
            writer.WriteLine();
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.WriteLine($"module {codeElement.Parent.Parent.Name.ToCamelCase()}");
                writer.IncreaseIndent();
            }
            var derivation = (codeElement.Inherits == null ? string.Empty : $" < {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation}");
            writer.IncreaseIndent();
            var mixins = (!codeElement.Implements.Any() ? string.Empty : $"include {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            writer.WriteLine($"{mixins}");
        }
    }
}
