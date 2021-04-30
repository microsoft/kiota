namespace Kiota.Builder.Writers.CSharp {
    public class CodeIndexerWriter : BaseCSharpElementWriter<CodeIndexer>
    {
        public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
        {
            var returnType = conventions.GetTypeString(codeElement.ReturnType);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType)} position] {{ get {{");
            writer.IncreaseIndent();
            conventions.AddRequestBuilderBody(returnType, writer, " + \"/\" + position", "return ");
            writer.DecreaseIndent();
            writer.WriteLine("} }");
        }
    }
}
