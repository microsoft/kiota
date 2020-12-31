using System;

namespace kiota.core
{
    public class CodeParameter : CodeTerminal, ICloneable
    {
        public CodeParameter(CodeElement parent): base(parent)
        {
            
        }
        public CodeType Type;
        public bool Optional = false;
        public bool IsQueryParameter { get; set; }

        public object Clone()
        {
            return new CodeParameter(Parent) {
                Optional = Optional,
                IsQueryParameter = IsQueryParameter,
                Name = Name.Clone() as string,
                Type = Type.Clone() as CodeType,
            };
        }
    }
}
