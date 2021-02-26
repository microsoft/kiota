using System;

namespace Kiota.Builder
{
    public enum CodePropertyKind
    {
        Custom,
        [Obsolete]
        ResponseHandler,
        RequestBuilder
    }

    public class CodeProperty : CodeTerminal
    {
        public CodeProperty(CodeElement parent): base(parent)
        {
            
        }
        public CodePropertyKind PropertyKind = CodePropertyKind.Custom;

        public override string Name
        {
            get; set;
        }
        public bool ReadOnly = false;
        public AccessModifier Access = AccessModifier.Public;
        public CodeType Type;
        public string DefaultValue;
    }
}
