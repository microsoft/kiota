using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, GoConventionService>
    {
        public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            if(codeElement?.Parent?.Parent is CodeNamespace ns) {
                writer.WriteLine($"package {ns.Name.Split('.').Last().ToLowerInvariant()}");
            }//TODO usings
            writer.WriteLine($"type {codeElement.Name.ToFirstCharacterUpperCase()} struct {{");
            writer.IncreaseIndent();
        }
    }
}
