namespace Kiota.Builder
{
    public class CodeIndexer : CodeTerminal, IDocumentedElement
    {
        public CodeIndexer(CodeElement parent): base(parent) {
            
        }
        public CodeTypeBase IndexType {get; set;}
        public CodeTypeBase ReturnType {get; set;}
        public string Description {get; set;}
    }
}
