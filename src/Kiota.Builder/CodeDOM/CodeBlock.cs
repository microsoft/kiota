using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;

/// <summary>
/// 
/// </summary>
public class CodeBlock<V, U> : CodeElement, IBlock where V : BlockDeclaration, new() where U : BlockEnd, new()
{
    public V StartBlock {get; set;}
    protected ConcurrentDictionary<string, CodeElement> InnerChildElements {get; private set;} = new (StringComparer.OrdinalIgnoreCase);
    public U EndBlock {get; set;}
    public CodeBlock():base()
    {
        StartBlock = new V { Parent = this };
        EndBlock = new U { Parent = this };
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
            InnerChildElements.TryRemove(element.Name, out _);
        }
    }
    public void RemoveUsingsByDeclarationName(params string[] names) => StartBlock.RemoveUsingsByDeclarationName(names);
    public void AddUsing(params CodeUsing[] codeUsings) => StartBlock.AddUsings(codeUsings);
    public IEnumerable<CodeUsing> Usings => StartBlock.Usings;
    protected IEnumerable<T> AddRange<T>(params T[] elements) where T : CodeElement {
        if(elements == null) return Enumerable.Empty<T>();
        EnsureElementsAreChildren(elements);
        var result = new T[elements.Length]; // not using yield return as they'll only get called if the result is assigned

        for(var i = 0; i < elements.Length; i++) {
            var element = elements[i];
            var returnedValue = InnerChildElements.GetOrAdd(element.Name, element);
            result[i] = HandleDuplicatedExceptions(element, returnedValue);
        }
        return result;
    }
    private T HandleDuplicatedExceptions<T>(T element, CodeElement returnedValue) where T: CodeElement {
        var added = returnedValue == element;
        if(!added && element is CodeMethod currentMethod)
            if(currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) &&
                returnedValue is CodeProperty cProp &&
                cProp.IsOfKind(CodePropertyKind.RequestBuilder)) {
                // indexer retrofitted to method in the parent request builder on the path and conflicting with the collection request builder property
                returnedValue = InnerChildElements.GetOrAdd($"{element.Name}-indexerbackcompat", element);
                added = true;
            } else if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor)) {
                // allows for methods overload
                var methodOverloadNameSuffix = currentMethod.Parameters.Any() ? currentMethod.Parameters.Select(x => x.Name).OrderBy(x => x).Aggregate((x, y) => x + y) : "1";
                returnedValue = InnerChildElements.GetOrAdd($"{element.Name}-{methodOverloadNameSuffix}", element);
                added = true;
            }

        if(!added && returnedValue.GetType() != element.GetType())
            throw new InvalidOperationException($"the current dom node already contains a child with name {returnedValue.Name} and of type {returnedValue.GetType().Name}");

        return returnedValue as T;
    }
    public IEnumerable<T> FindChildrenByName<T>(string childName) where T: ICodeElement {
        if(string.IsNullOrEmpty(childName))
            throw new ArgumentNullException(nameof(childName));

        if(InnerChildElements.Any()) {
            var result = new List<T>();
            var immediateResult = this.FindChildByName<T>(childName, false);
            if(immediateResult != null)
                result.Add(immediateResult);
            foreach(var childElement in InnerChildElements.Values.OfType<IBlock>())
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
            foreach(var childElement in InnerChildElements.Values.OfType<IBlock>()) {
                var childResult = childElement.FindChildByName<T>(childName, true);
                if(childResult != null)
                    return childResult;
            }
        return default;
    }
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
    public void RemoveUsings(params CodeUsing[] codeUsings)
    {
        if(codeUsings == null || codeUsings.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeUsings));
        foreach(var codeUsing in codeUsings)
            usings.Remove(codeUsing);
    }
    public void RemoveUsingsByDeclarationName(params string[] names) {
        if(names == null || names.Any(x => string.IsNullOrEmpty(x)))
            throw new ArgumentNullException(nameof(names));
        var namesAsHashset = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        usings.RemoveAll(x => namesAsHashset.Contains(x.Declaration?.Name));
    }
}
public class BlockEnd : CodeTerminal
{
}
