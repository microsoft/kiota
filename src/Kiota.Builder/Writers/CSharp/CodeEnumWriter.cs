using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, CSharpConventionService>
    {
        public CodeEnumWriter(CSharpConventionService conventionService):base(conventionService) {}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            if(!codeElement.Options.Any())
                return;

            var codeNamespace = codeElement?.Parent as CodeNamespace;
            if(codeNamespace != null) {
                writer.WriteLine($"namespace {codeNamespace.Name} {{");
                writer.IncreaseIndent();
            }
            if (codeElement.Flags)
                writer.WriteLine("[Flags]");
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public enum {codeElement.Name.ToFirstCharacterUpperCase()} {{"); //TODO docs
            writer.IncreaseIndent();
            writer.WriteLines(codeElement.Options
                            .Select(x => x.ToFirstCharacterUpperCase())
                            .Select((x, idx) => $"{x}{(codeElement.Flags ? " = " + GetEnumFlag(idx) : string.Empty)},")
                            .ToArray());
            if(codeNamespace != null)
                writer.CloseBlock();
        }
        private readonly Func<int, string> GetEnumFlag = (idx) => 
            (idx == 0 ? 1 : Math.Pow(2, idx)).ToString();
    }
}
