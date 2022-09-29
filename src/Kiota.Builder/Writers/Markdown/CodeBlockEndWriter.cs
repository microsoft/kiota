using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Markdown {
    public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, MarkdownConventionService>
    {
        public CodeBlockEndWriter(MarkdownConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            writer.CloseBlock();
            if(codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace) {
                writer.CloseBlock();
            }
        }
    }
}
