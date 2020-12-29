using System.Collections.Generic;

namespace kiota.core
{
    public class CodeUsing : CodeElement
    {
        public override string Name
        {
            get; set;
        }

        public override IList<CodeElement> GetChildElements()
        {
            return new List<CodeElement>();
        }
    }
}
