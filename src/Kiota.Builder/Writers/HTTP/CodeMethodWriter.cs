using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.http;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, HttpConventionService>
{
    public CodeMethodWriter(HttpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        // TODO (HTTP)
    }
}
