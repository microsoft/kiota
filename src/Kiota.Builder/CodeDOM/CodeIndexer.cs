namespace Kiota.Builder
{
    public class CodeIndexer : CodeTerminal, IDocumentedElement
    {
        private CodeTypeBase indexType;
        public CodeTypeBase IndexType {get => indexType; set {
            EnsureElementsAreChildren(value);
            indexType = value;
        }}
        private CodeTypeBase returnType;
        public CodeTypeBase ReturnType {get => returnType; set {
            EnsureElementsAreChildren(value);
            returnType = value;
        }}
        public string ParameterName { get; set; }
        public string Description {get; set;}
        /// <summary>
        /// The Path segment to use for the method name when using back-compatiable methods.
        /// </summary>
        public string PathSegment { get; set; }
    }
}
