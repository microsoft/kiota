using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Dart;
public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, DartConventionService>
{
    public CodeIndexerWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
    {
        throw new NotImplementedException();
    }
}
