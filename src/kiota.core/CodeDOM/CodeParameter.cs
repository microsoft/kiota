using System;

namespace kiota.core
{
    public class CodeParameter : CodeTerminal, ICloneable
    {
        public CodeType Type;
        public bool Optional = false;
        public bool IsQueryParameter { get; set; }

        public object Clone()
        {
            return new CodeParameter{
                Optional = Optional,
                IsQueryParameter = IsQueryParameter,
                Name = Name.Clone() as string,
                Type = Type.Clone() as CodeType,
            };
        }
    }
}
