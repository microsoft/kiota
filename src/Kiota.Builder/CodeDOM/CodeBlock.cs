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
        public IDictionary<string, CodeElement> InnerChildElements {get; set;} = new ConcurrentDictionary<string, CodeElement>(StringComparer.OrdinalIgnoreCase);
        public BlockEnd EndBlock {get; set;}
        public CodeBlock(CodeElement parent):base(parent)
        {
            StartBlock = new BlockDeclaration(this);
            EndBlock = new BlockEnd(this);
        }

        public override IEnumerable<CodeElement> GetChildElements()
        {
            return new CodeElement[] { StartBlock, EndBlock }.Union(InnerChildElements.Values);
        }
        public void AddUsing(params CodeUsing[] codeUsings)
        {
            if(!codeUsings.Any() || codeUsings.Any(x => x == null))
                throw new ArgumentOutOfRangeException(nameof(codeUsings));
            AddMissingParent(codeUsings);
            StartBlock.Usings.AddRange(codeUsings);
        }
        protected void AddRange(params CodeElement[] elements) {
            if(elements == null) return;
            
            foreach(var element in elements)
                try {
                    InnerChildElements.Add(element.Name, element); //try add is not behaving like documented
                } catch (ArgumentException) {
                    if(element is CodeMethod currentMethod) {// allows for methods overload
                        var methodOverloadNameSuffix = currentMethod.Parameters.Any() ? currentMethod.Parameters.Select(x => x.Name).OrderBy(x => x).Aggregate((x, y) => x + y) : "1";
                        InnerChildElements.Add($"{currentMethod.Name}-{methodOverloadNameSuffix}", currentMethod);
                    }
                }
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
