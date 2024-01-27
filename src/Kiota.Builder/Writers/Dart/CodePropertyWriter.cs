using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, DartConventionService>
{
    public CodePropertyWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        throw new NotImplementedException();
    }
}
