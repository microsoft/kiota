using System.Collections.Generic;

namespace kiota.core
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeNamespace : CodeBlock
    {
        private string name;

        public CodeNamespace()
        {
            StartBlock = new Declaration();
            EndBlock = new End();
        }
        public override string Name
        {
            get { return name;
            }
            set {
                name = value;
                StartBlock = new Declaration() { Name = name };
            }
        }

        public void AddClass(CodeClass codeClass)
        {
            this.InnerChildElements.Add(codeClass);
        }
        public void AddUsing(CodeUsing codeUsing)
        {
            ((Declaration)this.StartBlock).Usings.Add(codeUsing);
        }
        public void AddUsing(IEnumerable<CodeUsing> codeUsings)
        {
            foreach (var codeUsing in codeUsings)
            {
                ((Declaration)this.StartBlock).Usings.Add(codeUsing);
            }
        }
        public class Declaration : CodeTerminal
        {
            public override string Name { get; set; }
            public List<CodeUsing> Usings = new List<CodeUsing>();
        }

        public class End : CodeTerminal
        {

        }
    }
}
