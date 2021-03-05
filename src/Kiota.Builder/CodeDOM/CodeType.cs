using System;

namespace Kiota.Builder
{
    public class CodeType : CodeTypeBase, ICloneable
    {
        public CodeType(CodeElement parent): base(parent){
            
        }
        public CodeClass TypeDefinition
        {
            get;
            set;
        }
        public bool IsExternal = false;

        public override object Clone()
        {
            return new CodeType(this.Parent){
                Name = Name.Clone() as string,
                TypeDefinition = TypeDefinition,
                IsExternal = IsExternal
            }.BaseClone<CodeType>(this);
        }
    }
}
