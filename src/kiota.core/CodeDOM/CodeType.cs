using System;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CodeType : CodeTerminal, ICloneable
    {
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

        public OpenApiSchema Schema;

        public object Clone()
        {
            return new CodeType{
                ActionOf = ActionOf,
                Name = Name.Clone() as string,
                Schema = Schema,
                TypeDefinition = TypeDefinition,
            };
        }
    }
}
