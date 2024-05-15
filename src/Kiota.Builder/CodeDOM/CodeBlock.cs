using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// 
/// </summary>
public class CodeBlock<TBlockDeclaration, TBlockEnd> : CodeElement, IBlock where TBlockDeclaration : BlockDeclaration, new() where TBlockEnd : BlockEnd, new()
{
    public TBlockDeclaration StartBlock
    {
        get; set;
    }
    protected ConcurrentDictionary<string, CodeElement> InnerChildElements { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public TBlockEnd EndBlock
    {
        get; set;
    }
    public CodeBlock()
    {
        StartBlock = new TBlockDeclaration { Parent = this };
        EndBlock = new TBlockEnd { Parent = this };
    }
    public override IEnumerable<CodeElement> GetChildElements(bool innerOnly = false)
    {
        if (innerOnly)
            return InnerChildElements.Values;
        return new CodeElement[] { StartBlock, EndBlock }.Union(InnerChildElements.Values);
    }
    public virtual void RenameChildElement(string oldName, string newName)
    {
        if (InnerChildElements.TryRemove(oldName, out var element))
        {
            element.Name = newName;
            AddRange(element);
        }
        else throw new InvalidOperationException($"The element to rename was not found {oldName}");
    }
    public void RemoveChildElement<T>(params T[] elements) where T : ICodeElement
    {
        if (elements == null) return;
        RemoveChildElementByName(elements.Select(static x => x.Name).ToArray());
    }
    public virtual void RemoveChildElementByName(params string[] names)
    {
        if (names == null) return;

        foreach (var name in names)
        {
            InnerChildElements.TryRemove(name, out _);
        }
    }
    public void RemoveUsingsByDeclarationName(params string[] names) => StartBlock.RemoveUsingsByDeclarationName(names);
    public void AddUsing(params CodeUsing[] codeUsings) => StartBlock.AddUsings(codeUsings);
    [JsonIgnore]
    public IEnumerable<CodeUsing> Usings => StartBlock.Usings;
    protected IEnumerable<T> AddRange<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null) return [];
        EnsureElementsAreChildren(elements);
        var result = new T[elements.Length]; // not using yield return as they'll only get called if the result is assigned

        for (var i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            var returnedValue = InnerChildElements.GetOrAdd(element.Name, element);
            result[i] = HandleDuplicatedExceptions(element, returnedValue);
        }
        return result;
    }
    private T HandleDuplicatedExceptions<T>(T element, CodeElement returnedValue) where T : CodeElement
    {
        if (returnedValue == element)
            return element;
        if (element is CodeMethod currentMethod)
        {
            if (currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility) &&
                returnedValue is CodeProperty cProp &&
                cProp.IsOfKind(CodePropertyKind.RequestBuilder) &&
                InnerChildElements.GetOrAdd($"{element.Name}-indexerbackcompat", element) is T result)
            {
                // indexer retrofitted to method in the parent request builder on the path and conflicting with the collection request builder property
                return result;
            }
            else if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor) &&
                     returnedValue is CodeMethod existingMethod)
            {
                var currentMethodParameterNames = currentMethod.Parameters.Select(static x => x.Name).ToHashSet();
                var returnedMethodParameterNames = existingMethod.Parameters.Select(static x => x.Name).ToHashSet();
                if (currentMethodParameterNames.Count != returnedMethodParameterNames.Count ||
                    currentMethodParameterNames.Union(returnedMethodParameterNames)
                                                .Except(currentMethodParameterNames.Intersect(returnedMethodParameterNames))
                                                .Any())
                {
                    // allows for methods overload
                    var methodOverloadNameSuffix = currentMethodParameterNames.Count != 0 ? currentMethodParameterNames.OrderBy(static x => x).Aggregate(static (x, y) => x + y) : "1";
                    if (InnerChildElements.GetOrAdd($"{element.Name}-{methodOverloadNameSuffix}", element) is T result2)
                        return result2;
                }
            }
        }
        if (element is CodeProperty currentProperty &&
                currentProperty.Kind is CodePropertyKind.Custom &&
                returnedValue is CodeClass returnedClass && returnedClass.Kind is CodeClassKind.Model &&
                InnerChildElements.TryAdd($"{element.Name}-property", currentProperty))
            return element; // inline type property: transforming union type to wrapper class
        if (element is CodeClass currentClass &&
                currentClass.Kind is CodeClassKind.Model &&
                returnedValue is CodeProperty returnedProperty && returnedProperty.Kind is CodePropertyKind.Custom &&
                InnerChildElements.TryAdd($"{element.Name}-model", currentClass))
            return element; // inline type property: transforming wrapper class to union type
        if (element is CodeProperty conflictingProperty &&
                conflictingProperty.Kind is CodePropertyKind.Custom &&
                returnedValue is CodeMethod ctorMethod && ctorMethod.Kind is CodeMethodKind.Constructor)
        {
            if (string.IsNullOrEmpty(conflictingProperty.SerializationName))
            {
                conflictingProperty.SerializationName = conflictingProperty.Name;
            }
            if (InnerChildElements.TryAdd($"{element.Name}-property", conflictingProperty))
                return element; // property named constructor
        }
        if (element is CodeMethod ctorMethodBis &&
                ctorMethodBis.Kind is CodeMethodKind.Constructor &&
                returnedValue is CodeProperty conflictingPropertyBis && conflictingPropertyBis.Kind is CodePropertyKind.Custom)
        {
            if (string.IsNullOrEmpty(conflictingPropertyBis.SerializationName))
            {
                conflictingPropertyBis.SerializationName = conflictingPropertyBis.Name;
            }
            RenameChildElement(conflictingPropertyBis.Name, $"{conflictingPropertyBis.Name}-property");
            if (InnerChildElements.TryAdd(ctorMethodBis.Name, ctorMethodBis))
                return element; // property named constructor
        }
        if (element.GetType() == returnedValue.GetType())
            return (T)returnedValue;

        throw new InvalidOperationException($"the current dom node already contains a child with name {returnedValue.Name} and of type {returnedValue.GetType().Name}");
    }
    public IEnumerable<T> FindChildrenByName<T>(string childName) where T : ICodeElement
    {
        ArgumentException.ThrowIfNullOrEmpty(childName);

        if (!InnerChildElements.IsEmpty)
        {
            var result = new List<T>();
            var immediateResult = this.FindChildByName<T>(childName, false);
            if (immediateResult != null)
                result.Add(immediateResult);
            foreach (var childElement in InnerChildElements.Values.OfType<IBlock>())
                result.AddRange(childElement.FindChildrenByName<T>(childName));
            return result;
        }

        return Enumerable.Empty<T>();
    }
    public T? FindChildByName<T>(string childName, bool findInChildElements = true) where T : ICodeElement
    {
        return FindChildByName<T>(childName, findInChildElements ? uint.MaxValue : 1);
    }
    public T? FindChildByName<T>(string childName, uint maxDepth) where T : ICodeElement
    {
        ArgumentException.ThrowIfNullOrEmpty(childName);

        if (InnerChildElements.IsEmpty)
            return default;

        if (InnerChildElements.TryGetValue(childName, out var result) && result is T castResult)
            return castResult;
        if (--maxDepth > 0)
            foreach (var childElement in InnerChildElements.Values.OfType<IBlock>())
            {
                var childResult = childElement.FindChildByName<T>(childName, maxDepth);
                if (childResult != null)
                    return childResult;
            }
        return default;
    }
}
public class BlockDeclaration : CodeTerminal
{
    private readonly ConcurrentDictionary<CodeUsing, bool> usings = new(); // To avoid concurrent access issues
    [JsonIgnore]
    public IEnumerable<CodeUsing> Usings => usings.Keys;
    public void AddUsings(params CodeUsing[] codeUsings)
    {
        if (codeUsings == null || codeUsings.Any(static x => x == null))
            throw new ArgumentNullException(nameof(codeUsings));
        EnsureElementsAreChildren(codeUsings);
        foreach (var codeUsing in codeUsings)
            usings.TryAdd(codeUsing, true);
    }
    public void RemoveUsings(params CodeUsing[] codeUsings)
    {
        if (codeUsings == null || codeUsings.Any(static x => x == null))
            throw new ArgumentNullException(nameof(codeUsings));
        foreach (var codeUsing in codeUsings)
            usings.TryRemove(codeUsing, out var _);
    }
    public void RemoveUsingsByDeclarationName(params string[] names)
    {
        if (names == null || names.Any(string.IsNullOrEmpty))
            throw new ArgumentNullException(nameof(names));
        var namesAsHashSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var usingToRemove in usings.Keys.Where(x => !string.IsNullOrEmpty(x.Declaration?.Name) && namesAsHashSet.Contains(x.Declaration!.Name)))
            usings.TryRemove(usingToRemove, out var _);
    }
}
public class BlockEnd : CodeTerminal
{
}
