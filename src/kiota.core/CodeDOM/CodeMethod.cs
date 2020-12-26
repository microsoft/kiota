using System.Collections.Generic;

namespace kiota.core
{
    public class CodeMethod : CodeTerminal
    {
        public override string Name
        {
            get; set;
        }
        public string ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();

        internal void AddParameter(CodeParameter methodParameter)
        {
            Parameters.Add(methodParameter);
        }
    }
}
