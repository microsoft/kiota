using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    public class CodeEnum : CodeElement {
        public CodeEnum(CodeElement parent) : base(parent)
        {
            
        }
        public override IList<CodeElement> GetChildElements()
        {
            return Enumerable.Empty<CodeElement>().ToList();
        }
        public List<string> Options { get; set; } = new List<string>();
    }
}
