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
        public CodeNamespace(CodeElement parent):base(parent)
        {
            
        }
        private string name;
        public override string Name
        {
            get { return name;
            }
            set {
                name = value;
                if(StartBlock == null)
                    StartBlock = new BlockDeclaration(this);
                StartBlock.Name = name;
            }
        }

        public void AddClass(params CodeClass[] codeClasses)
        {
            if(!codeClasses.Any() || codeClasses.Any( x=> x == null))
                throw new ArgumentOutOfRangeException(nameof(codeClasses));
            AddMissingParent(codeClasses);
            this.InnerChildElements.AddRange(codeClasses);
        }
        public void AddNamespace(params CodeNamespace[] codeNamespaces) {
            if(!codeNamespaces.Any() || codeNamespaces.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeNamespaces));
            AddMissingParent(codeNamespaces);
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
        public CodeNamespace GetRootNamespace(CodeNamespace ns = null) {
            if(ns == null)
                ns = this;
            if (ns.Parent == null)
                return ns;
            else if(ns.Parent is CodeNamespace parent)
                return GetRootNamespace(parent);
            else
                throw new InvalidOperationException($"Found a namespace {ns.name} with a parent that's not a namespace {ns.Parent.Name} {ns.Parent.GetType()}");
        }
    }
}
