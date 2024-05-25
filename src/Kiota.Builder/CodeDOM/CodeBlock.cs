using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        var result = new List<CodeElement> { StartBlock, EndBlock };
        foreach (var innerChildElement in InnerChildElements.Values)
        {
            result.Add(innerChildElement);
        }

        return result;
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

        foreach (var element in elements)
        {
            if (element.Name is not null)
                InnerChildElements.TryRemove(element.Name, out _);
        }
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
                var currentMethodParameterNames = new HashSet<string>();
                foreach (var parameter in currentMethod.Parameters)
                {
                    currentMethodParameterNames.Add(parameter.Name);
                }
                var returnedMethodParameterNames = new HashSet<string>();
                foreach (var parameter in existingMethod.Parameters)
                {
                    returnedMethodParameterNames.Add(parameter.Name);
                }
                if (currentMethodParameterNames.Count != returnedMethodParameterNames.Count ||
                    currentMethodParameterNames.IsSupersetOf(returnedMethodParameterNames))
                {
                    // allows for methods overload
                    var methodOverloadNameSuffix = "1";
                    if (currentMethodParameterNames.Count != 0)
                    {
                        var list = new List<string>(currentMethodParameterNames);
                        list.Sort(StringComparer.CurrentCulture);
                        methodOverloadNameSuffix = string.Concat(list);
                    }
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
            foreach (var childElement in InnerChildElements.Values)
            {
                if (!(childElement is IBlock block)) continue;

                foreach (var foundChild in block.FindChildrenByName<T>(childName))
                {
                    result.Add(foundChild);
                }
            }
            return result;
        }

        return [];
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
            foreach (var childElement in InnerChildElements.Values)
            {
                if (!(childElement is IBlock block)) continue;

                var childResult = block.FindChildByName<T>(childName, maxDepth);
                if (childResult != null) return childResult;
            }
        return default;
    }
}
public class BlockDeclaration : CodeTerminal
{
    private readonly ConcurrentDictionary<CodeUsing, bool> usings = new(); // To avoid concurrent access issues

    public IEnumerable<CodeUsing> Usings => usings.Keys;

    public void AddUsings(params CodeUsing[] codeUsings)
    {
        ArgumentNullException.ThrowIfNull(codeUsings);

        foreach (var codeUsing in codeUsings)
        {
            if (codeUsing == null)
            {
                throw new ArgumentNullException(nameof(codeUsings), "One or more codeUsings is null.");
            }
        }

        EnsureElementsAreChildren(codeUsings);

        foreach (var codeUsing in codeUsings)
        {
            usings.TryAdd(codeUsing, true);
        }
    }

    public void RemoveUsings(params CodeUsing[] codeUsings)
    {
        ArgumentNullException.ThrowIfNull(codeUsings);

        foreach (var codeUsing in codeUsings)
        {
            if (codeUsing == null)
            {
                throw new ArgumentNullException(nameof(codeUsings), "One or more codeUsings is null.");
            }
        }

        foreach (var codeUsing in codeUsings)
        {
            usings.TryRemove(codeUsing, out var _);
        }
    }


    public void RemoveUsingsByDeclarationName(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        foreach (var name in names)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("One or more names is null or empty.", nameof(names));
            }
        }

        var namesAsHashSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

        foreach (var key in usings.Keys)
        {
            if (!string.IsNullOrEmpty(key.Declaration?.Name) && namesAsHashSet.Contains(key.Declaration!.Name))
            {
                usings.TryRemove(key, out _);
            }
        }
    }

}
public class BlockEnd : CodeTerminal
{
}
