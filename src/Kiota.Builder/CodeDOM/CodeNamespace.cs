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
        private CodeNamespace():base() {}
        public static CodeNamespace InitRootNamespace() {
            return new CodeNamespace();
        }
        private string name;
        public override string Name
        {
            get { return name;
            }
            set {
                name = value;
                StartBlock.Name = name;
            }
        }

        public IEnumerable<CodeClass> AddClass(params CodeClass[] codeClasses)
        {
            if(codeClasses == null || codeClasses.Any( x=> x == null))
                throw new ArgumentNullException(nameof(codeClasses));
            if(!codeClasses.Any())
                throw new ArgumentOutOfRangeException(nameof(codeClasses));
            return AddRange(codeClasses);
        }
        private static readonly char namespaceNameSeparator = '.';
        public CodeNamespace GetRootNamespace() {
            if (Parent is CodeNamespace parentNS) return parentNS.GetRootNamespace();
            else return this;
        }
        public IEnumerable<CodeNamespace> Namespaces => InnerChildElements.Values.OfType<CodeNamespace>();
        public IEnumerable<CodeClass> Classes => InnerChildElements.Values.OfType<CodeClass>();
        public IEnumerable<CodeEnum> Enums => InnerChildElements.Values.OfType<CodeEnum>();
        public CodeNamespace FindNamespaceByName(string nsName) {
            if(string.IsNullOrEmpty(nsName)) throw new ArgumentNullException(nameof(nsName));
            if(nsName.Equals(Name)) return this;
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
            var lastPresentSegmentNamespace = GetRootNamespace();
            while(lastPresentSegmentIndex < namespaceNameSegements.Length) {
                var segmentNameSpace = lastPresentSegmentNamespace.FindNamespaceByName(namespaceNameSegements.Take(lastPresentSegmentIndex + 1).Aggregate((x, y) => $"{x}.{y}"));
                if(segmentNameSpace == null)
                    break;
                else {
                    lastPresentSegmentNamespace = segmentNameSpace;
                    lastPresentSegmentIndex++;
                }
            }
            foreach(var childSegment in namespaceNameSegements.Skip(lastPresentSegmentIndex))
                lastPresentSegmentNamespace = lastPresentSegmentNamespace
                                            .AddRange(
                                                new CodeNamespace {
                                                    Name = $"{lastPresentSegmentNamespace?.Name}{(string.IsNullOrEmpty(lastPresentSegmentNamespace?.Name) ? string.Empty : ".")}{childSegment}",
                                                    Parent = lastPresentSegmentNamespace,
                                                    IsItemNamespace = childSegment.Equals(ItemNamespaceName, StringComparison.OrdinalIgnoreCase)
                                            }).First();
            return lastPresentSegmentNamespace;
        }
        private const string ItemNamespaceName = "item";
        public bool IsItemNamespace { get; private set; }
        public CodeNamespace EnsureItemNamespace() { 
            if (IsItemNamespace) return this;
            else if(string.IsNullOrEmpty(Name))
                throw new InvalidOperationException("adding an item namespace at the root is not supported");
            else {
                var childNamespace = InnerChildElements.Values.OfType<CodeNamespace>().FirstOrDefault(x => x.IsItemNamespace);
                if(childNamespace == null) {
                    childNamespace = AddNamespace($"{Name}.{ItemNamespaceName}");
                    childNamespace.IsItemNamespace = true;
                }
                return childNamespace;
            } 
        }
        public IEnumerable<CodeEnum> AddEnum(params CodeEnum[] enumDeclarations)
        {
            if(enumDeclarations == null || enumDeclarations.Any( x=> x == null))
                throw new ArgumentNullException(nameof(enumDeclarations));
            if(!enumDeclarations.Any())
                throw new ArgumentOutOfRangeException(nameof(enumDeclarations));
            return AddRange(enumDeclarations);
        }
        public int Depth { 
            get {
                if (Parent is CodeNamespace n) return n.Depth + 1;
                else return 0;
            }
        }
    }
}
