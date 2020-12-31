using System;
using System.Collections.Generic;
using System.Linq;

namespace kiota.core
{
    public class CodeMethod : CodeTerminal, ICloneable
    {
        public CodeMethod(CodeElement parent): base(parent)
        {
            
        }
        public CodeType ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();

        public object Clone()
        {
            return new CodeMethod(Parent) {
                ReturnType = ReturnType.Clone() as CodeType,
                Parameters = Parameters.Select(x => x.Clone() as CodeParameter).ToList(),
                Name = Name.Clone() as string,
            };
        }

        internal void AddParameter(params CodeParameter[] methodParameters)
        {
            if(!methodParameters.Any() || methodParameters.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(methodParameters));
            AddMissingParent(methodParameters);
            Parameters.AddRange(methodParameters);
        }
    }
}
