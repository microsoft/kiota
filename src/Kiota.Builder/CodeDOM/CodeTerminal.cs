using System.Collections.Generic;

namespace Kiota.Builder
{
    public class CodeTerminal : CodeElement
    {
        public CodeTerminal(CodeElement parent): base(parent)
        {
            
        }
        public override string Name
        {
            get;set;
        }

        public override IList<CodeElement> GetChildElements()
        {
            return new List<CodeElement>();
        }
    }
}
