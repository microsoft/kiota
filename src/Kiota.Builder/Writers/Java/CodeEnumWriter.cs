using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, JavaConventionService>
    {
        public CodeEnumWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            if(!codeElement.Options.Any())
                return;
            var enumName = codeElement.Name.ToFirstCharacterUpperCase();
            writer.WriteLines($"package {(codeElement.Parent as CodeNamespace)?.Name};",
                string.Empty,
                "import com.microsoft.kiota.serialization.ValuedEnum;",
                string.Empty);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public enum {enumName} implements ValuedEnum {{");
            writer.IncreaseIndent();
            writer.Write(codeElement.Options
                        .Select(x => $"{x.ToFirstCharacterUpperCase()}(\"{x}\")")
                        .Aggregate((x, y) => $"{x},{LanguageWriter.NewLine}{writer.GetIndent()}{y}") + ";" + LanguageWriter.NewLine);
            writer.WriteLines("public final String value;",
                $"{enumName}(final String value) {{");
            writer.IncreaseIndent();
            writer.WriteLine("this.value = value;");
            writer.DecreaseIndent();
            writer.WriteLines("}",
                        "@javax.annotation.Nonnull",
                        "public String getValue() { return this.value; }",
                        "@javax.annotation.Nullable",
                        $"public static {enumName} forValue(@javax.annotation.Nonnull final String searchValue) {{");
            writer.IncreaseIndent();
            writer.WriteLine("switch(searchValue) {");
            writer.IncreaseIndent();
            writer.Write(codeElement.Options
                        .Select(x => $"case \"{x}\": return {x.ToFirstCharacterUpperCase()};")
                        .Aggregate((x, y) => $"{x}{LanguageWriter.NewLine}{writer.GetIndent()}{y}") + LanguageWriter.NewLine);
            writer.WriteLine("default: return null;");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");

        }
    }
}
