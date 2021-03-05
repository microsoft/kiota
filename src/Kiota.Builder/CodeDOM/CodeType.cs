using System;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder
{
    public class CodeType : CodeTypeBase, ICloneable
    {
        public CodeType(CodeElement parent): base(parent){
            
        }
        public CodeType(CodeTypeBase ancestor): base(ancestor) {
        }
        public override string Name
        {
            get; set;
        }
        public CodeClass TypeDefinition
        {
            get;
            set;
        }
        public bool IsExternal = false;

        [Obsolete]
        public OpenApiSchema Schema;

        public new object Clone()
        {
            return new CodeType(base.Clone() as CodeTypeBase){
                Name = Name.Clone() as string,
                Schema = Schema,
                TypeDefinition = TypeDefinition,
                IsExternal = IsExternal
            };
        }
    }
}
