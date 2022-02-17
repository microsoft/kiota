namespace Kiota.Builder.Writers.CSharp {
    public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, CSharpConventionService>
    {
        public CodeBlockEndWriter(CSharpConventionService conventionService):base(conventionService){}
        public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
    }
}
