using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, GoConventionService>
    {
        public CodeEnumWriter(GoConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer) {
            if(codeElement?.Parent is CodeNamespace ns)
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().ToLowerInvariant()}");
            var typeName = codeElement.Name.ToFirstCharacterUpperCase();
            writer.WriteLines($"type {typeName} int",
                            string.Empty,
                            "const (");
            writer.IncreaseIndent();
            var isFirstOption = true;
            foreach (var item in codeElement.Options) {
                if (isFirstOption) {
                    isFirstOption = false;
                    writer.WriteLine($"{item.ToUpperInvariant()} {typeName} = iota");
                } else 
                    writer.WriteLine($"{item.ToUpperInvariant()}");
            }
            writer.DecreaseIndent();
            writer.WriteLines(")",
                            string.Empty,
                            $"func (i {typeName}) String() string {{");
            writer.IncreaseIndent();
            var literalOptions = codeElement.Options
                                            .Select(x => $"\"{x.ToUpperInvariant()}\"")
                                            .Aggregate((x, y) => x + ", " + y);
            writer.WriteLine($"return []string{{{literalOptions}}}[i]");
            writer.DecreaseIndent();
            writer.WriteLines("}");
        }
    }
}
