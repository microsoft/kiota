using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, SwiftConventionService>
    {
        public CodeEnumWriter(SwiftConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer) {
            if(!codeElement.Options.Any())
                return;

            var codeNamespace = codeElement?.Parent as CodeNamespace;
            if(codeNamespace != null) {
                writer.WriteLine($"extension {codeNamespace.Name} {{");
                writer.IncreaseIndent();
            }
            writer.WriteLine($"public enum {codeElement.Name.ToFirstCharacterUpperCase()} : String {{"); //TODO docs
            writer.IncreaseIndent();
            writer.WriteLines(codeElement.Options
                            .Select(x => x.ToFirstCharacterUpperCase())
                            .Select((x, idx) => $"case {x}")
                            .ToArray());
            writer.CloseBlock();//TODO static parse function?
            if(codeNamespace != null) {
                writer.CloseBlock();
            }
        }
    }
}
