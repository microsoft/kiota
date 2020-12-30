using System;
using System.Collections.Generic;
using System.Linq;

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
        public void AddNamespace(params CodeNamespace[] codeNamespaces) {
            if(!codeNamespaces.Any() || codeNamespaces.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeNamespaces));
            this.InnerChildElements.AddRange(codeNamespaces);
        }
        public bool IsRequestsNamespace { get; set; }
        public CodeNamespace RequestsNamespace { get => IsRequestsNamespace ? this : this.InnerChildElements.OfType<CodeNamespace>().FirstOrDefault(x => x.IsRequestsNamespace);}
        public CodeNamespace GetNamespace(string namespaceName) {
            if(string.IsNullOrEmpty(namespaceName)) 
                throw new ArgumentNullException(nameof(namespaceName));
            else if(Name.Equals(namespaceName, StringComparison.InvariantCultureIgnoreCase)) 
                return this;
            else if(this.InnerChildElements.OfType<CodeNamespace>().Any()) 
                return this.InnerChildElements.OfType<CodeNamespace>().FirstOrDefault(x => x.GetNamespace(namespaceName) != null);
            else 
                return null;
        }
    }
}
