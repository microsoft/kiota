using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, GoConventionService>
{
    public CodeEnumWriter(GoConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (!codeElement.Options.Any()) return;
        if (codeElement.Parent is CodeNamespace ns)
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)}");

        writer.StartBlock("import (");
        foreach (var cUsing in codeElement.Usings.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            writer.WriteLine($"\"{cUsing.Name}\"");
        writer.CloseBlock(")");

        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecation(codeElement, writer);
        writer.WriteLines($"type {typeName} int",
                        string.Empty,
                        "const (");
        writer.IncreaseIndent();
        var isMultiValue = codeElement.Flags;

        var enumOptions = codeElement.Options;
        int power = 0;
        foreach (var item in enumOptions)
        {
            if (item.Documentation.DescriptionAvailable)
                writer.WriteLine($"// {item.Documentation.DescriptionTemplate}");

            if (isMultiValue)
                writer.WriteLine($"{item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()} = {(int)Math.Pow(2, power)}");
            else
                writer.WriteLine($"{item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}{(power == 0 ? $" {typeName} = iota" : string.Empty)}");

            power++;
        }
        writer.DecreaseIndent();
        writer.WriteLines(")", string.Empty);

        WriteStringFunction(codeElement, writer, isMultiValue);
        WriteParsableEnum(codeElement, writer, isMultiValue);
        WriteSerializeFunction(codeElement, writer, isMultiValue);
        WriteMultiValueFunction(codeElement, writer, isMultiValue);
    }

    private void WriteStringFunction(CodeEnum codeElement, LanguageWriter writer, bool isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        var enumOptions = codeElement.Options.ToList();

        if (isMultiValue)
        {
            writer.StartBlock($"func (i {typeName}) String() string {{");
            writer.WriteLine("var values []string");
            var literalOptions = enumOptions
                .Select(x => $"\"{x.WireName}\"")
                .Aggregate((x, y) => x + ", " + y);
            writer.WriteLine($"options := []string{{{literalOptions}}}");
            writer.StartBlock($"for p := 0; p < {enumOptions.Count}; p++ {{");
            writer.WriteLine($"mantis := {typeName}(int(math.Pow(2, float64(p))))");
            writer.StartBlock($"if i&mantis == mantis {{");
            writer.WriteLine($"values = append(values, options[p])");
            writer.CloseBlock();
            writer.CloseBlock();
            writer.WriteLine("return strings.Join(values, \",\")");
            writer.CloseBlock();
        }
        else
        {
            writer.StartBlock($"func (i {typeName}) String() string {{");
            var literalOptions = enumOptions
                .Select(x => $"\"{x.WireName}\"")
                .Aggregate((x, y) => x + ", " + y);
            writer.WriteLine($"return []string{{{literalOptions}}}[i]");
            writer.CloseBlock();
        }
    }

    private void WriteParsableEnum(CodeEnum codeElement, LanguageWriter writer, Boolean isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        var enumOptions = codeElement.Options;

        writer.StartBlock($"func Parse{typeName}(v string) (any, error) {{");

        if (isMultiValue)
        {
            writer.WriteLine($"var result {typeName}");
            writer.WriteLine("values := strings.Split(v, \",\")");
            writer.StartBlock("for _, str := range values {");
            writer.StartBlock("switch str {");
            foreach (var item in enumOptions)
            {
                writer.StartBlock($"case \"{item.WireName}\":");
                writer.WriteLine($"result |= {item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
                writer.DecreaseIndent();
            }
        }
        else
        {
            writer.WriteLine($"result := {enumOptions.First().Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
            writer.StartBlock("switch v {");
            foreach (var item in enumOptions)
            {
                writer.StartBlock($"case \"{item.WireName}\":");
                writer.WriteLine($"result = {item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
                writer.DecreaseIndent();
            }
        }


        writer.StartBlock("default:");
        writer.WriteLine($"return 0, errors.New(\"Unknown {typeName} value: \" + v)");
        writer.DecreaseIndent();
        writer.CloseBlock();
        if (isMultiValue) writer.CloseBlock();
        writer.WriteLine("return &result, nil");
        writer.CloseBlock();
    }

    private void WriteSerializeFunction(CodeEnum codeElement, LanguageWriter writer, Boolean isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.StartBlock($"func Serialize{typeName}(values []{typeName}) []string {{");
        writer.WriteLines("result := make([]string, len(values))",
            "for i, v := range values {");
        writer.IncreaseIndent();
        writer.WriteLine("result[i] = v.String()");
        writer.CloseBlock();
        writer.WriteLine("return result");
        writer.CloseBlock();
    }

    private void WriteMultiValueFunction(CodeEnum codeElement, LanguageWriter writer, Boolean isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.StartBlock($"func (i {typeName}) isMultiValue() bool {{");
        writer.WriteLine($"return {isMultiValue.ToString().ToLowerInvariant()}");
        writer.CloseBlock();
    }
}
