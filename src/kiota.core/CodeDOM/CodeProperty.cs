namespace kiota.core
{
    public class CodeProperty : CodeTerminal
    {
        public CodeProperty(CodeElement parent): base(parent)
        {
            
        }
        public override string Name
        {
            get; set;
        }
        public bool ReadOnly = false;
        public CodeType Type;
    }
}
