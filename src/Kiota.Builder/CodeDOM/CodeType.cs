using System;
using Microsoft.OpenApi.Models;

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

        [Obsolete]
        public OpenApiSchema Schema;

        public override object Clone()
        {
            return new CodeType(this.Parent){
                Name = Name.Clone() as string,
                Schema = Schema,
                TypeDefinition = TypeDefinition,
                IsExternal = IsExternal
            }.BaseClone<CodeType>(this);
        }
    }
}
