using System.Collections.Generic;

namespace kiota.core
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeNamespace : CodeBlock
    {
        private string name;
        public override string Name
        {
            get { return name;
            }
            set {
                name = value;
                if(StartBlock == null)
                    StartBlock = new BlockDeclaration();
                StartBlock.Name = name;
            }
        }

        public void AddClass(CodeClass codeClass)
        {
            this.InnerChildElements.Add(codeClass);
        }
    }
}
