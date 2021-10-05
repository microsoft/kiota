using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder
{
    /// <summary>
    /// Abstract element of some piece of source code to be generated
    /// </summary>
    public abstract class CodeElement : ICodeElement
    {
        public CodeElement Parent { get; set; }
        public int GetNamespaceDepth(int currentDepth = 0) {
            return this switch {
                _ when Parent is null => currentDepth,
                CodeNamespace ns => ns.Parent.GetNamespaceDepth(++currentDepth),
                _ => Parent.GetNamespaceDepth(currentDepth),
            };
        }
        public virtual IEnumerable<CodeElement> GetChildElements(bool innerOnly = false) => Enumerable.Empty<CodeElement>();

        public virtual string Name
        {
            get; set;
        }
        protected void EnsureElementsAreChildren(params CodeElement[] elements) {
            foreach(var element in elements.Where(x => x != null && (x.Parent == null || x.Parent != this)))
                element.Parent = this;
        }
        public T GetImmediateParentOfType<T>(CodeElement item = null) {
            if(item == null)
                return GetImmediateParentOfType<T>(this);
            else if (item is T p)
                return p;
            else if (item.Parent == null)
                throw new InvalidOperationException($"item {item.Name} of type {item.GetType()} does not have a parent");
            else if(item.Parent is T p2)
                return p2;
            else
                return GetImmediateParentOfType<T>(item.Parent);
        }
        public bool IsChildOf(CodeElement codeElement, bool immediateOnly = false) {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            else if(this.Parent == codeElement) return true;
            else if(immediateOnly || this.Parent == null) return false;
            else return this.Parent.IsChildOf(codeElement, immediateOnly);
        }
    }
}
