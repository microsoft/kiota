using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, GoConventionService>
    {
        public CodeEnumWriter(GoConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer) {
            if(!codeElement.Options.Any()) return;
            if(codeElement?.Parent is CodeNamespace ns)
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment()}");

            writer.WriteLine("import (");
            writer.IncreaseIndent();
            foreach(var cUsing in codeElement.Usings)
                writer.WriteLine($"\"{cUsing.Name}\"");
            writer.DecreaseIndent();
            writer.WriteLine(")");
            var typeName = codeElement.Name.ToFirstCharacterUpperCase();
            writer.WriteLines($"type {typeName} int",
                            string.Empty,
                            "const (");
            writer.IncreaseIndent();
            var iotaSuffix = $" {typeName} = iota";
            foreach (var item in codeElement.Options) {
                writer.WriteLine($"{item.ToUpperInvariant()}_{typeName.ToUpperInvariant()}{iotaSuffix}");
                if (!string.IsNullOrEmpty(iotaSuffix))
                    iotaSuffix = string.Empty;
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
            writer.WriteLines("}",
                            $"func Parse{typeName}(v string) (interface{{}}, error) {{");
            writer.IncreaseIndent();
            writer.WriteLine($"switch v {{");
            writer.IncreaseIndent();
            foreach (var item in codeElement.Options) {
                writer.WriteLine($"case \"{item.ToUpperInvariant()}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"return {item.ToUpperInvariant()}_{typeName.ToUpperInvariant()}, nil");
                writer.DecreaseIndent();
            }
            writer.DecreaseIndent();
            writer.WriteLines("}",
                            $"return 0, errors.New(\"Unkown {typeName} value: \" + v)");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
