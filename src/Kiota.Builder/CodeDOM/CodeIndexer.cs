using System;

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
        public string Description {get; set;}
    }
}
