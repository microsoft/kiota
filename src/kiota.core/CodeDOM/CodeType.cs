using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CodeType : CodeTerminal
    {
        public override string Name
        {
            get; set;
        }
        public bool ActionOf = false;

        public OpenApiSchema Schema;
    }
}
