using System;

namespace Kiota.Builder
{
    public class CodeIndexer : CodeTerminal, IDocumentedElement
    {
        public CodeIndexer(): base() {}
        private CodeTypeBase indexType;
        public CodeTypeBase IndexType {get => indexType; set {
            AddMissingParent(value);
            indexType = value;
        }}
        private CodeTypeBase returnType;
        public CodeTypeBase ReturnType {get => returnType; set {
            AddMissingParent(value);
            returnType = value;
        }}
        public string Description {get; set;}
    }
}
