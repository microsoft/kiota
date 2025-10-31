﻿using System;
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
        {
            // always add a comment to the top of the file to indicate it's generated
            conventions.WriteGeneratorComment(writer);
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)}");
        }

        var usings = codeElement.Usings.Select(static x => x.Name).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (usings.Count > 0)
        {
            writer.StartBlock("import (");
            usings.ForEach(x => writer.WriteLine($"\"{x}\""));
            writer.CloseBlock(")");
        }

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
                conventions.WriteDescriptionItem(item.Documentation.DescriptionTemplate, writer);

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
        WriteSerializeFunction(codeElement, writer);
        WriteMultiValueFunction(codeElement, writer, isMultiValue);
    }

    private static void WriteStringFunction(CodeEnum codeElement, LanguageWriter writer, bool isMultiValue)
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
            writer.WriteLine("values = append(values, options[p])");
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

    private static void WriteParsableEnum(CodeEnum codeElement, LanguageWriter writer, Boolean isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        var enumOptions = codeElement.Options.ToArray();

        writer.StartBlock($"func Parse{typeName}(v string) (any, error) {{");

        if (isMultiValue)
        {
            writer.WriteLine($"var result {typeName}");
            writer.WriteLine("values := strings.Split(v, \",\")");
            writer.StartBlock("for _, str := range values {");
            writer.WriteLine("switch str {");
            foreach (var item in enumOptions)
            {
                writer.WriteLine($"case \"{item.WireName}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"result |= {item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
                writer.DecreaseIndent();
            }
            writer.WriteLine("default:");
            writer.IncreaseIndent();
            writer.WriteLine("return nil, nil");
            writer.DecreaseIndent();
            writer.WriteLine("}"); // close the switch statement
            writer.CloseBlock(); // close the for loop
        }
        else
        {
            writer.WriteLine($"result := {enumOptions[0].Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
            writer.WriteLine("switch v {");
            foreach (var item in enumOptions)
            {
                writer.WriteLine($"case \"{item.WireName}\":");
                writer.IncreaseIndent();
                writer.WriteLine($"result = {item.Name.ToUpperInvariant()}_{typeName.ToUpperInvariant()}");
                writer.DecreaseIndent();
            }
            writer.StartBlock("default:");
            writer.WriteLine("return nil, nil");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        writer.WriteLine("return &result, nil");
        writer.CloseBlock();
    }

    private static void WriteSerializeFunction(CodeEnum codeElement, LanguageWriter writer)
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

    private static void WriteMultiValueFunction(CodeEnum codeElement, LanguageWriter writer, Boolean isMultiValue)
    {
        var typeName = codeElement.Name.ToFirstCharacterUpperCase();
        writer.StartBlock($"func (i {typeName}) isMultiValue() bool {{");
        writer.WriteLine($"return {isMultiValue.ToString().ToLowerInvariant()}");
        writer.CloseBlock();
    }
}
