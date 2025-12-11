using System;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Java;

public class CodeEnumWriter : BaseElementWriter<CodeEnum, JavaConventionService>
{
    public CodeEnumWriter(JavaConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var enumOptions = codeElement.Options.ToArray();
        if (enumOptions.Length == 0)
            return;
        var enumName = codeElement.Name;
        writer.WriteLines($"package {(codeElement.Parent as CodeNamespace)?.Name};",
            string.Empty,
            "import com.microsoft.kiota.serialization.ValuedEnum;",
            "import java.util.Objects;",
            string.Empty);
        conventions.WriteLongDescription(codeElement, writer);
        conventions.WriteDeprecatedAnnotation(codeElement, writer);
        writer.WriteLine($"{JavaConventionService.AutoGenerationHeader}");
        writer.WriteLine($"public enum {enumName} implements ValuedEnum {{");
        writer.IncreaseIndent();
        var lastEnumOption = enumOptions.Last();
        foreach (var enumOption in enumOptions)
        {
            conventions.WriteShortDescription(enumOption, writer);
            writer.WriteLine($"{enumOption.Name}(\"{enumOption.SerializationName}\"){(enumOption == lastEnumOption ? ";" : ",")}");
        }
        writer.WriteLines("public final String value;",
            $"{enumName}(final String value) {{");
        writer.IncreaseIndent();
        writer.WriteLine("this.value = value;");
        writer.DecreaseIndent();
        writer.WriteLines("}",
                    "@jakarta.annotation.Nonnull",
                    "public String getValue() { return this.value; }",
                    "@jakarta.annotation.Nullable",
                    $"public static {enumName} forValue(@jakarta.annotation.Nonnull final String searchValue) {{");
        writer.IncreaseIndent();
        writer.WriteLines("Objects.requireNonNull(searchValue);",
                        "switch(searchValue) {");
        writer.IncreaseIndent();
        writer.Write(enumOptions
                    .Select(x => $"case \"{x.WireName}\": return {x.Name};")
                    .Aggregate((x, y) => $"{x}{LanguageWriter.NewLine}{writer.GetIndent()}{y}") + LanguageWriter.NewLine);
        writer.WriteLine("default: return null;");
        writer.CloseBlock();
        writer.CloseBlock();
        writer.CloseBlock();
    }
}
