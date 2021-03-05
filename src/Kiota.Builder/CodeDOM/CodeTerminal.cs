using System.Collections.Generic;

namespace Kiota.Builder
{
    public abstract class CodeTerminal : CodeElement
    {
        public CodeTerminal(CodeElement parent): base(parent)
        {
            
        }
        public override IList<CodeElement> GetChildElements()
        {
            return new List<CodeElement>();
        }
    }
}
