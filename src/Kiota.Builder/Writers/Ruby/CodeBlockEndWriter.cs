namespace Kiota.Builder.Writers.Ruby {
    public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, RubyConventionService>
    {
        private string end = "end";
        public CodeBlockEndWriter(RubyConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            if(codeElement.Parent is CodeEnum) return;
            writer.CloseBlock(end);
            if(codeElement?.Parent is CodeClass codeClass && codeClass.Parent is CodeNamespace) {
                writer.CloseBlock(end);
            }
        }
    }
}
