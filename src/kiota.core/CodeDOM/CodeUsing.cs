using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kiota.core.CodeDOM
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
