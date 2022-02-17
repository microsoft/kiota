namespace Kiota.Builder.Writers.Ruby {
    public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, RubyConventionService>
    {
        public CodeBlockEndWriter(RubyConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            const string end = "end";
            writer.DecreaseIndent();
            writer.WriteLine(end);
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine(end);
            }
        }
    }
}
