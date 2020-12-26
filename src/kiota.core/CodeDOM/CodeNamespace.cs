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
        public string Name
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

        public class Declaration : CodeTerminal
        {
            public string Name;
        }

        public class End : CodeTerminal
        {

        }
    }
}
