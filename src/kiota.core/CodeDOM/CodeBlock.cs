using System.Collections.Generic;

namespace kiota.core
{

    /// <summary>
    /// 
    /// </summary>
    public class CodeBlock : CodeElement
    {
        public CodeElement StartBlock;
        public List<CodeElement> InnerChildElements = new List<CodeElement>();
        public CodeElement EndBlock;

        public override string Name
        {
            get;set;
        }

        public override IList<CodeElement> GetChildElements()
        {
            var elements = new List<CodeElement>(InnerChildElements);
            elements.Insert(0, StartBlock);
            elements.Add(EndBlock);
            return elements;
        }
    }
}
