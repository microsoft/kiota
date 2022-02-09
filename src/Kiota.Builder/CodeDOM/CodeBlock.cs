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
        public CodeBlock():base()
        {
            StartBlock = new BlockDeclaration() { Parent = this };
            EndBlock = new BlockEnd() { Parent = this};
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
        public void AddUsing(params CodeUsing[] codeUsings) => StartBlock.AddUsings(codeUsings);
        protected IEnumerable<T> AddRange<T>(params T[] elements) where T : CodeElement {
            if(elements == null) return Enumerable.Empty<T>();
            EnsureElementsAreChildren(elements);
            var innerChildElements = InnerChildElements as ConcurrentDictionary<string, CodeElement>; // to avoid calling the non thread-safe extension method
            var result = new T[elements.Length]; // not using yield return as they'll only get called if the result is assigned

            for(var i = 0; i < elements.Length; i++) {
                var element = elements[i];
                var returnedValue = innerChildElements.GetOrAdd(element.Name, element);
                result[i] = (T)HandleDuplicatedExceptions(innerChildElements, element, returnedValue);
            }
            return result;
        }
        private static CodeElement HandleDuplicatedExceptions(ConcurrentDictionary<string, CodeElement> innerChildElements, CodeElement element, CodeElement returnedValue) {
            var added = returnedValue == element;
            if(!added && element is CodeMethod currentMethod)
                if(currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) &&
                    returnedValue is CodeProperty cProp &&
                    cProp.IsOfKind(CodePropertyKind.RequestBuilder)) {
                    // indexer retrofited to method in the parent request builder on the path and conflicting with the collection request builder propeerty
                    returnedValue = innerChildElements.GetOrAdd($"{element.Name}-indexerbackcompat", element);
                    added = true;
                } else if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
                    // allows for methods overload
                    var methodOverloadNameSuffix = currentMethod.Parameters.Any() ? currentMethod.Parameters.Select(x => x.Name).OrderBy(x => x).Aggregate((x, y) => x + y) : "1";
                    returnedValue = innerChildElements.GetOrAdd($"{element.Name}-{methodOverloadNameSuffix}", element);
                    added = true;
                }

            if(!added && returnedValue.GetType() != element.GetType())
                throw new InvalidOperationException($"the current dom node already contains a child with name {returnedValue.Name} and of type {returnedValue.GetType().Name}");

            return returnedValue;
        }
        public IEnumerable<T> FindChildrenByName<T>(string childName) where T: ICodeElement {
            if(string.IsNullOrEmpty(childName))
                throw new ArgumentNullException(nameof(childName));

            if(InnerChildElements.Any()) {
                var result = new List<T>();
                var immediateResult = this.FindChildByName<T>(childName, false);
                if(immediateResult != null)
                    result.Add(immediateResult);
                foreach(var childElement in InnerChildElements.Values.OfType<CodeBlock>())
                    result.AddRange(childElement.FindChildrenByName<T>(childName));
                return result;
            } else
                return Enumerable.Empty<T>();
        }
        public T FindChildByName<T>(string childName, bool findInChildElements = true) where T: ICodeElement {
            if(string.IsNullOrEmpty(childName))
                throw new ArgumentNullException(nameof(childName));
            
            if(!InnerChildElements.Any())
                return default;

            if(InnerChildElements.TryGetValue(childName, out var result) && result is T castResult)
                return castResult;
            else if(findInChildElements)
                foreach(var childElement in InnerChildElements.Values.OfType<CodeBlock>()) {
                    var childResult = childElement.FindChildByName<T>(childName, true);
                    if(childResult != null)
                        return childResult;
                }
            return default;
        }
        public class BlockDeclaration : CodeTerminal
        {
            private readonly List<CodeUsing> usings = new ();
            public IEnumerable<CodeUsing> Usings => usings;
            public void AddUsings(params CodeUsing[] codeUsings) {
                if(codeUsings == null || codeUsings.Any(x => x == null))
                    throw new ArgumentNullException(nameof(codeUsings));
                EnsureElementsAreChildren(codeUsings);
                usings.AddRange(codeUsings);
            }
            public void RemoveUsingsByDeclarationName(string name) {
                if(string.IsNullOrEmpty(name))
                    throw new ArgumentNullException(nameof(name));
                usings.RemoveAll(x => name.Equals(x.Declaration?.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public class BlockEnd : CodeTerminal
        {
        }
    }
}
