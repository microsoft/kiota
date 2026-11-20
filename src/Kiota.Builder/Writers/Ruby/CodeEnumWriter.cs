using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby;

public class CodeEnumWriter : BaseElementWriter<CodeEnum, RubyConventionService>
{
    public CodeEnumWriter(RubyConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine(RubyConventionService.FrozenStringLiteralComment);
        if (!codeElement.Options.Any())
            return;
        writer.WriteLine();
        if (codeElement.Parent is CodeNamespace ns)
            conventions.WriteNamespaceModules(ns, writer);
        conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecationComment(codeElement, writer);
        writer.StartBlock($"{codeElement.Name.ToFirstCharacterUpperCase()} = {{");
        // the value is what goes on the wire, the key is what Ruby code looks the member up by
        var options = codeElement.Options.Select(x => $"{x.Name.ToFirstCharacterUpperCase()}: {(codeElement.IsNumeric ? GetRubyNumericLiteral(codeElement, x) : RubyConventionService.ToRubyStringLiteral(x.WireName))}").ToArray();
        for (var i = 0; i < options.Length; i++)
            writer.WriteLine(i < options.Length - 1 ? $"{options[i]}," : options[i]);
        writer.CloseBlock("}.freeze");
        if (codeElement.Parent is CodeNamespace ns2)
            conventions.WriteNamespaceClosing(ns2, writer);
    }
    private static string GetRubyNumericLiteral(CodeEnum codeElement, CodeEnumOption option)
    {
        if (long.TryParse(option.WireName, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var rubyIntVal))
        {
            return rubyIntVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (double.TryParse(option.WireName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rubyDoubleVal))
        {
            return rubyDoubleVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new InvalidOperationException($"Invalid numeric enum value '{option.WireName}' for option '{option.Name}' in enum '{codeElement.Name}'.");
    }
}
