using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Markdown;
public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, MarkdownConventionService>
{
    public CodeIndexerWriter(MarkdownConventionService conventionService) : base(conventionService) {}
    public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        writer.WriteLine($" this[{conventions.GetTypeString(codeElement.IndexType, codeElement)}] {returnType}] ");
       conventions.WriteLongDescription(codeElement.Documentation, writer);
    }
}
