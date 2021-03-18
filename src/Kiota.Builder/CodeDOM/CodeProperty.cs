using System;

namespace Kiota.Builder
{
    public enum CodePropertyKind
    {
        Custom,
        RequestBuilder,
        Serializer,
        Deserializer
    }

    public class CodeProperty : CodeTerminal
    {
        public CodeProperty(CodeElement parent): base(parent)
        {
            
        }
        public CodePropertyKind PropertyKind = CodePropertyKind.Custom;
        public bool ReadOnly = false;
        public AccessModifier Access = AccessModifier.Public;
        public CodeTypeBase Type;
        public string DefaultValue;
    }
}
