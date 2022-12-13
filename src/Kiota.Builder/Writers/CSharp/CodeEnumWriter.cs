using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, CSharpConventionService>
{
    public CodeEnumWriter(CSharpConventionService conventionService):base(conventionService) {}
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if(!codeElement.Options.Any())
            return;

        var codeNamespace = codeElement.Parent as CodeNamespace;
        if(codeNamespace != null)
            writer.StartBlock($"namespace {codeNamespace.Name} {{");
        if (codeElement.Flags)
            writer.WriteLine("[Flags]");
        conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
        writer.WriteLine($"public enum {codeElement.Name.ToFirstCharacterUpperCase()} {{");
        writer.IncreaseIndent();
        var idx = 0;
        foreach(var option in codeElement.Options) {
            conventions.WriteShortDescription(option.Documentation.Description, writer);
            writer.WriteLine($"{option.Name.ToFirstCharacterUpperCase()}{(codeElement.Flags ? " = " + GetEnumFlag(idx) : string.Empty)},");
            idx++;
        }
        if(codeNamespace != null)
            writer.CloseBlock();
    }
    private static readonly Func<int, string> GetEnumFlag = static idx => 
        (idx == 0 ? 1 : Math.Pow(2, idx)).ToString();
}
