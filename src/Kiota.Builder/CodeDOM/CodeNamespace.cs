using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeNamespace : CodeBlock
    {
        private CodeNamespace(CodeElement parent):base(parent) {}
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
            AddRange(codeClasses);
        }
        private void AddNamespace(params CodeNamespace[] codeNamespaces) {
            if(!codeNamespaces.Any() || codeNamespaces.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeNamespaces));
            AddMissingParent(codeNamespaces);
            AddRange(codeNamespaces);
        }
        private static readonly char namespaceNameSeparator = '.';
        private CodeNamespace GetRootNamespace() {
            if(Parent == null) return this;
            else return (this.Parent as CodeNamespace).GetRootNamespace();
        }
        public CodeNamespace FindNamespaceByName(string nsName) {
            var result = FindChildByName<CodeNamespace>(nsName, false);
            if(result == null)
                foreach(var childNS in InnerChildElements.Values.OfType<CodeNamespace>()) {
                    result = childNS.FindNamespaceByName(nsName);
                    if(result != null)
                        break;
                }
            return result;
        }
        public CodeNamespace AddNamespace(string namespaceName) {
            if(string.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));
            var namespaceNameSegements = namespaceName.Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
            var lastPresentSegmentIndex = default(int);
            CodeNamespace lastPresentSegmentNamespace = Parent == null ? this : GetRootNamespace();
            while(lastPresentSegmentIndex < namespaceNameSegements.Length) {
                var segmentNameSpace = lastPresentSegmentNamespace.FindNamespaceByName(namespaceNameSegements.Take(lastPresentSegmentIndex + 1).Aggregate((x, y) => $"{x}.{y}"));
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
        public CodeNamespace EnsureItemNamespace() { 
            if (IsItemNamespace) return this;
            else {
                var childNamespace = this.InnerChildElements.Values.OfType<CodeNamespace>().FirstOrDefault(x => x.IsItemNamespace);
                if(childNamespace == null) {
                    childNamespace = AddNamespace($"{this.Name}.item");
                    childNamespace.IsItemNamespace = true;
                }
                return childNamespace;
            } 
        }
        public void AddEnum(params CodeEnum[] enumDeclarations)
        {
            if(!enumDeclarations.Any() || enumDeclarations.Any( x=> x == null))
                throw new ArgumentOutOfRangeException(nameof(enumDeclarations));
            AddMissingParent(enumDeclarations);
            AddRange(enumDeclarations);
        }
    }
}
