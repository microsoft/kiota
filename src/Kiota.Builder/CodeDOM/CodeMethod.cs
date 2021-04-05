using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeMethodKind
    {
        Custom,
        IndexerBackwardCompatibility,
        RequestExecutor
    }

    public class CodeMethod : CodeTerminal, ICloneable
    {
        public CodeMethod(CodeElement parent): base(parent)
        {
            
        }
        public CodeMethodKind MethodKind = CodeMethodKind.Custom;
        public AccessModifier Access = AccessModifier.Public;
        public CodeType ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();
        public bool IsStatic = false;
        public bool IsAsync = true;

        public object Clone()
        {
            return new CodeMethod(Parent) {
                MethodKind = MethodKind,
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
