using System;

namespace kiota.core
{
    public enum CodeParameterKind
    {
        Custom,
        QueryParameter
    }

    public class CodeParameter : CodeTerminal, ICloneable
    {
        public CodeParameterKind ParameterKind = CodeParameterKind.Custom;
        public CodeType Type;
        public bool Optional = false;

        public object Clone()
        {
            return new CodeParameter{
                Optional = Optional,
                ParameterKind = ParameterKind,
                Name = Name.Clone() as string,
                Type = Type.Clone() as CodeType,
            };
        }
    }
}
