using System;
using System.Collections.Generic;
using System.Linq;

namespace kiota.core
{
    public class CodeMethod : CodeTerminal, ICloneable
    {
        public CodeType ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();

        public object Clone()
        {
            return new CodeMethod {
                ReturnType = ReturnType.Clone() as CodeType,
                Parameters = Parameters.Select(x => x.Clone() as CodeParameter).ToList(),
                Name = Name.Clone() as string,
            };
        }

        internal void AddParameter(CodeParameter methodParameter)
        {
            Parameters.Add(methodParameter);
        }
    }
}
