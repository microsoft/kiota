using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public enum CodeMethodKind
    {
        Custom,
        IndexerBackwardCompatibility,
        RequestExecutor,
        RequestGenerator,
        Serializer
    }
    public enum HttpMethod {
        Get,
        Post,
        Patch,
        Put,
        Delete,
        Options,
        Connect,
        Head,
        Trace
    }

    public class CodeMethod : CodeTerminal, ICloneable
    {
        public CodeMethod(CodeElement parent): base(parent)
        {
            
        }
        public HttpMethod? HttpMethod;
        public CodeMethodKind MethodKind = CodeMethodKind.Custom;
        public AccessModifier Access = AccessModifier.Public;
        public CodeTypeBase ReturnType;
        public List<CodeParameter> Parameters = new List<CodeParameter>();
        public bool IsStatic = false;
        public bool IsAsync = true;

        public object Clone()
        {
            return new CodeMethod(Parent) {
                MethodKind = MethodKind,
                ReturnType = ReturnType.Clone() as CodeTypeBase,
                Parameters = Parameters.Select(x => x.Clone() as CodeParameter).ToList(),
                Name = Name.Clone() as string,
                HttpMethod = HttpMethod,
                IsAsync = IsAsync,
                Access = Access,
                IsStatic = IsStatic,
                GenerationProperties = new (GenerationProperties),
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
