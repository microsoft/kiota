using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, DartConventionService>
{
    public CodeEnumWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        throw new NotImplementedException();
    }
}
