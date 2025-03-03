using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Crystal;
public class CodeTypeWriter : BaseElementWriter<CodeType, CrystalConventionService>
{
    public CodeTypeWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        
        writer.WriteLine(conventions.GetTypeString(codeElement, codeElement));
    }
}
