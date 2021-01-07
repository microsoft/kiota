using System;
using System.Collections.Generic;
using System.Linq;

namespace kiota.core
{

    /// <summary>
    /// 
    /// </summary>
    public class CodeBlock : CodeElement
    {
        public BlockDeclaration StartBlock;
        public List<CodeElement> InnerChildElements = new List<CodeElement>();
        public BlockEnd EndBlock;
        public CodeBlock(CodeElement parent):base(parent)
        {
            StartBlock = new BlockDeclaration(this);
            EndBlock = new BlockEnd(this);
        }

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
            if(!codeUsings.Any() || codeUsings.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeUsings));
            AddMissingParent(codeUsings);
            StartBlock.Usings.AddRange(codeUsings);
        }
        public T GetChildElementOfType<T>(Func<T,bool> predicate) where T : CodeBlock {
            if(predicate == null) 
                throw new ArgumentNullException(nameof(predicate));
            else if(this is T thisT && predicate(thisT)) 
                return thisT;
            else if (this.InnerChildElements.OfType<T>().Any(predicate))
                return this.InnerChildElements.OfType<T>().First(predicate);
            else if(this.InnerChildElements.OfType<CodeBlock>().Any())
                return this.InnerChildElements.OfType<CodeBlock>()
                                                .Select(x => x.GetChildElementOfType<T>(predicate))
                                                .OfType<T>()
                                                .FirstOrDefault();
            else 
                return null;
        }
        public class BlockDeclaration : CodeTerminal
        {
            public override string Name { get; set; }
            public List<CodeUsing> Usings = new List<CodeUsing>();
            public BlockDeclaration(CodeElement parent): base(parent)
            {
                
            }
        }

        public class BlockEnd : CodeTerminal
        {
            public BlockEnd(CodeElement parent): base(parent)
            {
                
            }
        }
    }
}
