using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kiota.Builder
{

    /// <summary>
    /// 
    /// </summary>
    public class CodeBlock : CodeElement
    {
        public BlockDeclaration StartBlock {get; set;}
        protected IDictionary<string, CodeElement> InnerChildElements {get; private set;} = new ConcurrentDictionary<string, CodeElement>(StringComparer.OrdinalIgnoreCase);
        public BlockEnd EndBlock {get; set;}
        public CodeBlock(CodeElement parent):base(parent)
        {
            StartBlock = new BlockDeclaration(this);
            EndBlock = new BlockEnd(this);
        }

        public override IEnumerable<CodeElement> GetChildElements(bool innerOnly = false)
        {
            if(innerOnly)
                return InnerChildElements.Values;
            else
                return new CodeElement[] { StartBlock, EndBlock }.Union(InnerChildElements.Values);
        }
        public void RemoveChildElement<T>(params T[] elements) where T: CodeElement {
            if(elements == null) return;

            foreach(var element in elements) {
                InnerChildElements.Remove(element.Name);
            }
        }
        public void AddUsing(params CodeUsing[] codeUsings)
        {
            if(!codeUsings.Any() || codeUsings.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeUsings));
            AddMissingParent(codeUsings);
            StartBlock.Usings.AddRange(codeUsings);
        }
        protected IEnumerable<T> AddRange<T>(params T[] elements) where T : CodeElement {
            if(elements == null) return Enumerable.Empty<T>();
            AddMissingParent(elements);
            var innerChildElements = InnerChildElements as ConcurrentDictionary<string, CodeElement>; // to avoid calling the non thread-safe extension method
            var result = new T[elements.Length]; // not using yield return as they'll only get called if the result is assigned

            for(var i = 0; i < elements.Length; i++) {
                var element = elements[i];
                var addedValue = innerChildElements.GetOrAdd(element.Name, element);
                if(addedValue != element)
                    if(!(addedValue is T))
                        throw new InvalidOperationException($"the current dom node already contains a child with name {addedValue.Name} and of type {addedValue.GetType().Name}");
                    else if(element is CodeMethod currentMethod) { // allows for methods overload
                        var methodOverloadNameSuffix = currentMethod.Parameters.Any() ? currentMethod.Parameters.Select(x => x.Name).OrderBy(x => x).Aggregate((x, y) => x + y) : "1";
                        addedValue = innerChildElements.GetOrAdd($"{element.Name}-{methodOverloadNameSuffix}", currentMethod);
                    }
                result[i] = (T)addedValue;
            }
            return result;
        }
        public T FindChildByName<T>(string childName, bool findInChildElements = true) where T: ICodeElement {
            if(string.IsNullOrEmpty(childName))
                throw new ArgumentNullException(nameof(childName));
            
            if(!InnerChildElements.Any())
                return default(T);

            if(InnerChildElements.TryGetValue(childName, out var result) && result is T)
                return (T)(object)result;
            else if(findInChildElements)
                foreach(var childElement in InnerChildElements.Values.OfType<CodeBlock>()) {
                    var childResult = childElement.FindChildByName<T>(childName, true);
                    if(childResult != null)
                        return childResult;
                }
            return default(T);
        }
        public class BlockDeclaration : CodeTerminal
        {
            public List<CodeUsing> Usings {get; set;} = new List<CodeUsing>();
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
