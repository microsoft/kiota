using System;
using System.Linq;

namespace Kiota.Builder
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeNamespace : CodeBlock
    {
        private CodeNamespace(CodeElement parent):base(parent)
        {
            
        }
        public static CodeNamespace InitRootNamespace() {
            return new CodeNamespace(null);
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
        private void AddNamespace(params CodeNamespace[] codeNamespaces) {
            if(!codeNamespaces.Any() || codeNamespaces.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeNamespaces));
            AddMissingParent(codeNamespaces);
            this.InnerChildElements.AddRange(codeNamespaces);
        }
        private static readonly char namespaceNameSeparator = '.';
        public CodeNamespace AddNamespace(string namespaceName) {
            if(string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));
            if(this.Parent != null)
                throw new InvalidOperationException("adding namespaces is only supported from the root namespace");
            var namespaceNameSegements = namespaceName.Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var lastPresentSegmentIndex = default(int);
            CodeNamespace lastPresentSegmentNamespace = this;
            while(lastPresentSegmentIndex < namespaceNameSegements.Length) {
                var segmentNameSpace = this.GetNamespace(namespaceNameSegements.Take(lastPresentSegmentIndex + 1).Aggregate((x, y) => $"{x}.{y}"));
                if(segmentNameSpace == null)
                    break;
                else {
                    lastPresentSegmentNamespace = segmentNameSpace;
                    lastPresentSegmentIndex++;
                }
            }
            if(lastPresentSegmentNamespace != null)
                foreach(var childSegment in namespaceNameSegements.Skip(lastPresentSegmentIndex)) {
                    var newNS = new CodeNamespace(lastPresentSegmentNamespace) {
                        Name = $"{lastPresentSegmentNamespace?.Name}{(string.IsNullOrEmpty(lastPresentSegmentNamespace?.Name) ? string.Empty : ".")}{childSegment}",
                    };
                    lastPresentSegmentNamespace.AddNamespace(newNS);
                    lastPresentSegmentNamespace = newNS;
                }
            return lastPresentSegmentNamespace;
        }
        public bool IsItemNamespace { get; private set; }
        public CodeNamespace EnsureItemNamespace(CodeNamespace rootNamespace) { 
            if(rootNamespace.Parent != null)
                throw new ArgumentException("ensuring item namespaces is only supported when passing the root namespace as a parameter", nameof(rootNamespace));
            if (IsItemNamespace) return this;
            else {
                var childNamespace = this.InnerChildElements.OfType<CodeNamespace>().FirstOrDefault(x => x.IsItemNamespace);
                if(childNamespace == null) {
                    childNamespace = rootNamespace.AddNamespace($"{this.Name}.item");
                    childNamespace.IsItemNamespace = true;
                }
                return childNamespace;
            } 
        }
        public CodeNamespace GetNamespace(string namespaceName) {
            if(string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));
            else
                return this.GetChildElementOfType<CodeNamespace>(x => x.Name?.Equals(namespaceName, StringComparison.InvariantCultureIgnoreCase) ?? false);
        }
        public void AddEnum(params CodeEnum[] enumDeclarations)
        {
            if(!enumDeclarations.Any() || enumDeclarations.Any( x=> x == null))
                throw new ArgumentOutOfRangeException(nameof(enumDeclarations));
            AddMissingParent(enumDeclarations);
            this.InnerChildElements.AddRange(enumDeclarations);
        }
    }
}
