using System;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodePropertyKind
    {
        Custom,
        RequestBuilder,
        AdditionalData,
        BackingStore,
        PathSegment,
        CurrentPath,
        HttpCore,
        RawUrl
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
        public string SerializationName { get; set; }
        public string NamePrefix { get; set; }
        public bool IsOfKind(params CodePropertyKind[] kinds) {
            return kinds?.Contains(PropertyKind) ?? false;
        }
    }
}
