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

        internal void AddParameter(params CodeParameter[] methodParameters)
        {
            if(!methodParameters.Any() || methodParameters.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(methodParameters));
            Parameters.AddRange(methodParameters);
        }
    }
}
