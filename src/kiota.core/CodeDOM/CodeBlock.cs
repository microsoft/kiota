using System.Collections.Generic;

namespace kiota.core
{

    /// <summary>
    /// 
    /// </summary>
    public class CodeBlock : CodeElement
    {
        public BlockDeclaration StartBlock = new BlockDeclaration();
        public List<CodeElement> InnerChildElements = new List<CodeElement>();
        public BlockEnd EndBlock = new BlockEnd();

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
        public void AddUsing(params CodeUsing[] codeUsings)
        {
            StartBlock.Usings.AddRange(codeUsings);
        }
        public class BlockDeclaration : CodeTerminal
        {
            public override string Name { get; set; }
            public List<CodeUsing> Usings = new List<CodeUsing>();
        }

        public class BlockEnd : CodeTerminal
        {

        }
    }
}
