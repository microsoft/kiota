namespace kiota.core
{
    public enum CodePropertyKind
    {
        Custom,
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
