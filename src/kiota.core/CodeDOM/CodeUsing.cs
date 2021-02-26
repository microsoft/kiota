using System.Collections.Generic;

namespace Kiota.Builder
{
    public class CodeUsing : CodeElement
    {
        public CodeUsing(CodeElement parent): base(parent)
        {
            
        }
        public override string Name
        {
            get; set;
        }
        public CodeType Declaration { get; set; }

        public override IList<CodeElement> GetChildElements()
        {
            return new List<CodeElement>();
        }
    }
}
