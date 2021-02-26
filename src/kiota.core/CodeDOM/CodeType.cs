using System;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CodeType : CodeTerminal, ICloneable
    {
        public CodeType(CodeElement parent): base(parent)
        {
            
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

        public bool ActionOf = false;
        public bool IsNullable = true;

        public OpenApiSchema Schema;

        public object Clone()
        {
            return new CodeType(Parent){
                ActionOf = ActionOf,
                Name = Name.Clone() as string,
                Schema = Schema,
                TypeDefinition = TypeDefinition,
            };
        }
    }
}
