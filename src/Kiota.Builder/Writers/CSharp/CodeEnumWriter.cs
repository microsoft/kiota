using System;
using System.Globalization;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, CSharpConventionService>
{
    public CodeEnumWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (!codeElement.Options.Any())
            return;

        var codeNamespace = codeElement.Parent as CodeNamespace;
        if (codeNamespace != null)
        {
            foreach (var x in codeElement.Usings
                    .Where(static x => x.Declaration?.IsExternal ?? true)
                    .Select(static x => $"using {(x.Declaration?.Name ?? x.Name).NormalizeNameSpaceName(".")};")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal))
                writer.WriteLine(x);
            writer.StartBlock($"namespace {codeNamespace.Name} {{");
        }
        if (codeElement.Flags)
            writer.WriteLine("[Flags]");
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        var deprecationMessage = conventions.GetDeprecationInformation(codeElement);
        if (!string.IsNullOrEmpty(deprecationMessage))
            writer.WriteLine(deprecationMessage);
        writer.StartBlock($"public enum {codeElement.Name.ToFirstCharacterUpperCase()} {{");
        var idx = 0;
        foreach (var option in codeElement.Options)
        {
            conventions.WriteShortDescription(option.Documentation.Description, writer);

            if (option.IsNameEscaped)
            {
                writer.WriteLine($"[EnumMember(Value = \"{option.SerializationName}\")]");
            }
            writer.WriteLine($"{option.Name.ToFirstCharacterUpperCase()}{(codeElement.Flags ? " = " + GetEnumFlag(idx) : string.Empty)},");
            idx++;
        }
        if (codeNamespace != null)
            writer.CloseBlock();
    }
    private static readonly Func<int, string> GetEnumFlag = static idx =>
        (idx == 0 ? 1 : Math.Pow(2, idx)).ToString(CultureInfo.InvariantCulture);
}
