using System;

namespace Kiota.Builder
{
    public class CodeType : CodeTypeBase, ICloneable
    {
        public CodeElement TypeDefinition
        {
            get;
            set;
        }
        public bool IsExternal {get;set;} = false;

        public override object Clone()
        {
            return new CodeType{
                TypeDefinition = TypeDefinition,
                IsExternal = IsExternal
            }.BaseClone<CodeType>(this);
        }
    }
}
