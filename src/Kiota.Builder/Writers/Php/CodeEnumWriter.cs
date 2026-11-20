using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php;

public partial class CodeEnumWriter : BaseElementWriter<CodeEnum, PhpConventionService>
{
    public CodeEnumWriter(PhpConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        conventions.WritePhpDocumentStart(writer);
        var enumProperties = codeElement.Options;
        if (codeElement.Parent is CodeNamespace enumNamespace)
        {
            writer.WriteLine($"namespace {enumNamespace.Name.ReplaceDotsWithSlashInNamespaces()};");
        }
        writer.WriteLine();
        var hasUse = false;
        foreach (var use in codeElement.Usings)
        {
            codeElement.Usings?
                .Where(x => x.Declaration != null && (x.Declaration.IsExternal ||
                                                        !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.Declaration is { IsExternal: true }
                    ? $"use {x.Declaration.Name.ReplaceDotsWithSlashInNamespaces()}\\{x.Name.ReplaceDotsWithSlashInNamespaces()};"
                    : $"use {x.Name.ReplaceDotsWithSlashInNamespaces()}\\{x.Declaration!.Name.ReplaceDotsWithSlashInNamespaces()};")
                .Distinct()
                .Order()
                .ToList()
                .ForEach(x =>
                {
                    hasUse = true;
                    writer.WriteLine(x);
                });
        }
        if (hasUse)
        {
            writer.WriteLine(string.Empty);
        }
        writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()} extends Enum {{");
        writer.IncreaseIndent();
        var reservedNames = enumProperties.Select(static option => GetEnumValueName(option.Name)).ToHashSet(StringComparer.Ordinal);
        var emittedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var enumProperty in enumProperties)
        {
            var constantName = GetEnumValueName(enumProperty.Name);
            if (!emittedNames.Add(constantName))
            {
                var suffix = 2;
                var candidate = $"{constantName}_{suffix}";
                while (reservedNames.Contains(candidate) || !emittedNames.Add(candidate))
                    candidate = $"{constantName}_{++suffix}";
                constantName = candidate;
            }
            if (codeElement?.IsNumeric ?? false)
            {
                if (long.TryParse(enumProperty.WireName, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var phpIntVal))
                {
                    writer.WriteLine($"public const {constantName} = {phpIntVal.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
                }
                else if (double.TryParse(enumProperty.WireName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var phpDoubleVal))
                {
                    writer.WriteLine($"public const {constantName} = {phpDoubleVal.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
                }
                else
                {
                    throw new InvalidOperationException($"Invalid numeric enum value '{enumProperty.WireName}' for option '{enumProperty.Name}' in enum '{codeElement.Name}'.");
                }
            }
            else
            {
                writer.WriteLine($"public const {constantName} = \"{PhpConventionService.SanitizePhpDoubleQuoteLiteral(enumProperty.WireName)}\";");
            }
        }
    }
    [GeneratedRegex(@"([A-Z]{1})", RegexOptions.Singleline, 500)]
    private static partial Regex _enumValueNameRegex();
    private static string GetEnumValueName(string original)
    {
        return _enumValueNameRegex().Replace(original, "_$1").Trim('_').ToUpperInvariant();
    }

}
