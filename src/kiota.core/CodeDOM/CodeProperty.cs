namespace kiota.core
{
    public class CodeProperty : CodeTerminal
    {
        public override string Name
        {
            get; set;
        }
        public bool ReadOnly = false;
        public CodeType Type;
        public string DefaultValue;
    }
}
