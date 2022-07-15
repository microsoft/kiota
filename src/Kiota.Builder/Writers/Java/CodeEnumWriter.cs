using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Java {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, JavaConventionService>
    {
        public CodeEnumWriter(JavaConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            var enumOptions = codeElement.Options.ToArray();
            if(!enumOptions.Any())
                return;
            var enumName = codeElement.Name.ToFirstCharacterUpperCase();
            writer.WriteLines($"package {(codeElement.Parent as CodeNamespace)?.Name};",
                string.Empty,
                "import com.microsoft.kiota.serialization.ValuedEnum;",
                "import java.util.Objects;",
                string.Empty);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public enum {enumName} implements ValuedEnum {{");
            writer.IncreaseIndent();
            var lastEnumOption = enumOptions.Last();
            foreach(var enumOption in enumOptions) {
                conventions.WriteShortDescription(enumOption.Description, writer);
                writer.WriteLine($"{enumOption.Name.ToFirstCharacterUpperCase()}(\"{enumOption.Name}\"){(enumOption == lastEnumOption ? ";" : ",")}");
            }
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
            writer.WriteLines("Objects.requireNonNull(searchValue);",
                            "switch(searchValue) {");
            writer.IncreaseIndent();
            writer.Write(enumOptions
                        .Select(x => $"case \"{x.SerializationName ?? x.Name}\": return {x.Name.ToFirstCharacterUpperCase()};")
                        .Aggregate((x, y) => $"{x}{LanguageWriter.NewLine}{writer.GetIndent()}{y}") + LanguageWriter.NewLine);
            writer.WriteLine("default: return null;");
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }
    }
}
