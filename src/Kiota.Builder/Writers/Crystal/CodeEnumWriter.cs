using System;
using System.Globalization;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Crystal;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, CrystalConventionService>
{
    public CodeEnumWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        
        writer.WriteLine(CodeClassDeclarationWriter.AutoGenerationHeader);
        
        if (codeElement.Parent is CodeNamespace parentNamespace)
        {
            writer.WriteLine($"module {parentNamespace.Name.ToFirstCharacterUpperCase()}");
            writer.IncreaseIndent();
        }
        
        conventions.WriteLongDescription(codeElement, writer);
        writer.WriteLine($"enum {codeElement.Name.ToFirstCharacterUpperCase()}");
        writer.IncreaseIndent();
        
        foreach (var option in codeElement.Options.OrderBy(static x => x.Name))
        {
            conventions.WriteShortDescription(option, writer);
            writer.WriteLine($"{option.Name.ToFirstCharacterUpperCase()}");
        }
        
        writer.DecreaseIndent();
        writer.WriteLine("end");
        
        if (codeElement.Parent is CodeNamespace)
        {
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
    }
}
