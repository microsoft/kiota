using System;

namespace Kiota.Builder
{
    public enum CodePropertyKind
    {
        Custom,
        RequestBuilder,
        Deserializer
    }

    public class CodeProperty : CodeTerminal, IDocumentedElement
    {
        public CodeProperty(CodeElement parent): base(parent)
        {
            
        }
        public CodePropertyKind PropertyKind {get;set;} = CodePropertyKind.Custom;
        public bool ReadOnly {get;set;} = false;
        public AccessModifier Access {get;set;} = AccessModifier.Public;
        public CodeTypeBase Type {get;set;}
        public string DefaultValue {get;set;}
        public string Description {get; set;}
    }
}
