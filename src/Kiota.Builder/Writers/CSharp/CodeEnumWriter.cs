﻿using System;
using System.Globalization;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, CSharpConventionService>
{
    public static string AutoGenerationHeader => "// <auto-generated/>";
    public static string GeneratedCodeAttribute { get; } = $"[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\", \"{Kiota.Generated.KiotaVersion.Current()}\")]";
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
            writer.WriteLine(AutoGenerationHeader);
            foreach (var x in codeElement.Usings
                    .Where(static x => x.Declaration?.IsExternal ?? true)
                    .Select(static x => $"using {(x.Declaration?.Name ?? x.Name).NormalizeNameSpaceName(".")};")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal))
                writer.WriteLine(x);
            writer.WriteLine($"namespace {codeNamespace.Name}");
            writer.StartBlock();
        }
        bool hasDescription = conventions.WriteShortDescription(codeElement, writer);
        writer.WriteLine(GeneratedCodeAttribute);
        if (codeElement.Flags)
            writer.WriteLine("[Flags]");
        conventions.WriteDeprecationAttribute(codeElement, writer);
        if (!hasDescription) writer.WriteLine("#pragma warning disable CS1591");
        writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} enum {codeElement.Name.ToFirstCharacterUpperCase()}");
        if (!hasDescription) writer.WriteLine("#pragma warning restore CS1591");
        writer.StartBlock();
        var idx = 0;
        foreach (var option in codeElement.Options)
        {
            hasDescription = conventions.WriteShortDescription(option, writer);

            if (option.IsNameEscaped)
            {
                writer.WriteLine($"[EnumMember(Value = \"{option.SerializationName}\")]");
            }
            if (!hasDescription) writer.WriteLine("#pragma warning disable CS1591");
            writer.WriteLine($"{option.Name.ToFirstCharacterUpperCase()}{(codeElement.Flags ? " = " + GetEnumFlag(idx) : string.Empty)},");
            if (!hasDescription) writer.WriteLine("#pragma warning restore CS1591");
            idx++;
        }
        if (codeNamespace != null)
            writer.CloseBlock();
    }
    private static readonly Func<int, string> GetEnumFlag = static idx =>
        (idx == 0 ? 1 : Math.Pow(2, idx)).ToString(CultureInfo.InvariantCulture);
}
