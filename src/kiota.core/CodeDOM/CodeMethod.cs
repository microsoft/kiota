using System.Collections.Generic;

namespace kiota.core
{
    public class CodeMethod : CodeTerminal
    {
        public CodeType ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();

        internal void AddParameter(CodeParameter methodParameter)
        {
            Parameters.Add(methodParameter);
        }
    }
}
