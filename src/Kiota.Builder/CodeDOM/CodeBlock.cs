using System;
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
        public Dictionary<string, CodeElement> InnerChildElements {get; set;} = new();
        public BlockEnd EndBlock {get; set;}
        public CodeBlock(CodeElement parent):base(parent)
        {
            StartBlock = new BlockDeclaration(this);
            EndBlock = new BlockEnd(this);
        }

        public override IList<CodeElement> GetChildElements()
        {
            var elements = new List<CodeElement>(InnerChildElements.Values);
            elements.Insert(0, StartBlock);
            elements.Add(EndBlock);
            return elements;
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
                if(!InnerChildElements.TryAdd(element.Name, element))
                    if(element is CodeMethod currentMethod) {// allows for methods overload
                        var methodOverloadNameSuffix = currentMethod.Parameters.Any() ? currentMethod.Parameters.Select(x => x.Name).OrderBy(x => x).Aggregate((x, y) => x + y) : "1";
                        InnerChildElements.Add($"{currentMethod.Name}-{methodOverloadNameSuffix}", currentMethod);
                    }
        }
        public T FindChildByName<T>(string childName, bool findInChildElements = true) where T: CodeElement {
            if(string.IsNullOrEmpty(childName))
                throw new ArgumentNullException(nameof(childName));
            
            if(!InnerChildElements.Any())
                return null;

            if(InnerChildElements.TryGetValue(childName, out var result) && result is T)
                return (T)result;
            else if(findInChildElements)
                foreach(var childElement in InnerChildElements.Values.OfType<CodeBlock>()) {
                    var childResult = childElement.FindChildByName<T>(childName, true);
                    if(childResult != null)
                        return childResult;
                }
            return null;
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
