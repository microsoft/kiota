using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, GoConventionService>
    {
        public CodeEnumWriter(GoConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer) {
            if(!codeElement.Options.Any()) return;
            if(codeElement?.Parent is CodeNamespace ns)
                writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty)}");

            writer.WriteLine("import (");
            writer.IncreaseIndent();
            foreach(var cUsing in codeElement.Usings)
                writer.WriteLine($"\"{cUsing.Name}\"");
            writer.DecreaseIndent();
            writer.WriteLine(")");
            var typeName = codeElement.Name.ToFirstCharacterUpperCase();
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLines($"type {typeName} int",
                            string.Empty,
                            "const (");
            writer.IncreaseIndent();
            var iotaSuffix = $" {typeName} = iota";
            var enumOptions = codeElement.Options;
            foreach (var item in enumOptions) {
                if(!string.IsNullOrEmpty(item.Description))
                    writer.WriteLine($"// {item.Description}");
                writer.WriteLine($"{item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}{iotaSuffix}");
                if (!string.IsNullOrEmpty(iotaSuffix))
                    iotaSuffix = string.Empty;
            }
            writer.DecreaseIndent();
            writer.WriteLines(")",
                            string.Empty,
                            $"func (i {typeName}) String() string {{");
            writer.IncreaseIndent();
            var literalOptions = enumOptions
                                            .Select(x => $"\"{x.SerializationName ?? x.Name}\"")
                                            .Aggregate((x, y) => x + ", " + y);
            writer.WriteLine($"return []string{{{literalOptions}}}[i]");
            writer.DecreaseIndent();
            writer.WriteLines("}",
                            $"func Parse{typeName}(v string) (interface{{}}, error) {{");
            writer.IncreaseIndent();
            writer.WriteLine($"result := {enumOptions.First().Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
            writer.WriteLine($"switch v {{");
            writer.IncreaseIndent();
            foreach (var item in enumOptions) {
                writer.WriteLine($"case \"{item.SerializationName ?? item.Name}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"result = {item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
                writer.DecreaseIndent();
            }
            writer.WriteLine("default:");
            writer.IncreaseIndent();
            writer.WriteLine($"return 0, errors.New(\"Unknown {typeName} value: \" + v)");
            writer.DecreaseIndent();
            writer.CloseBlock();
            writer.WriteLine("return &result, nil");
            writer.CloseBlock();
            writer.WriteLine($"func Serialize{typeName}(values []{typeName}) []string {{");
            writer.IncreaseIndent();
            writer.WriteLines("result := make([]string, len(values))",
                                "for i, v := range values {");
            writer.IncreaseIndent();
            writer.WriteLine("result[i] = v.String()");
            writer.CloseBlock();
            writer.WriteLine("return result");
            writer.CloseBlock();
        }
    }
}
