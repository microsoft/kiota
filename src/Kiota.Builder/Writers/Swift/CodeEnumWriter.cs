using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, SwiftConventionService>
    {
        public CodeEnumWriter(SwiftConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer) {
            if(!codeElement.Options.Any())
                return;

            if (codeElement.Parent is CodeNamespace codeNamespace)
            {
                writer.StartBlock($"extension {codeNamespace.Name} {{");
            }
            writer.StartBlock($"public enum {codeElement.Name.ToFirstCharacterUpperCase()} : String {{"); //TODO docs
            writer.WriteLines(codeElement.Options
                            .Select(static x => x.Name.ToFirstCharacterUpperCase())
                            .Select(static (x, idx) => $"case {x}")
                            .ToArray());
            //TODO static parse function?
            //enum and ns are closed by the code block end writer
        }
    }
}
