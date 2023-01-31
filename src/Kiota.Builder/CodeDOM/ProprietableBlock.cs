using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// Marker interface for type testing
/// </summary>
public interface IProprietableBlock : ICodeElement
{
}

/// <summary>
/// Represents a block of code that can have properties and methods
/// </summary>
public abstract class ProprietableBlock<T, U> : CodeBlock<U, BlockEnd>, IDocumentedElement, IProprietableBlock where T : Enum where U : ProprietableBlockDeclaration, new()
{
    private string name = string.Empty;
    /// <summary>
    /// Name of Class
    /// </summary>
    public override string Name
    {
        get => name;
        set
        {
            name = value;
            StartBlock.Name = name;
        }
    }
    public CodeDocumentation Documentation { get; set; } = new();
    public virtual IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if (properties == null || properties.Any(x => x == null))
            throw new ArgumentNullException(nameof(properties));
        if (!properties.Any())
            throw new ArgumentOutOfRangeException(nameof(properties));
        return AddRange(properties);
    }
#nullable disable
    public T Kind
    {
        get; set;
    }
#nullable enable
    public bool IsOfKind(params T[] kinds)
    {
        return kinds?.Contains(Kind) ?? false;
    }
    public CodeProperty? GetPropertyOfKind(params CodePropertyKind[] kind) =>
    Properties.FirstOrDefault(x => x.IsOfKind(kind));

    public CodeProperty? GetMethodByAccessedPropertyOfKind(params CodePropertyKind[] kind) =>
        Methods.FirstOrDefault(x => x.AccessedProperty?.IsOfKind(kind) ?? false)?.AccessedProperty;
    
    public IEnumerable<CodeProperty> Properties => InnerChildElements.Values.OfType<CodeProperty>().OrderBy(static x => x.Name);
    public IEnumerable<CodeMethod> Methods => InnerChildElements.Values.OfType<CodeMethod>().OrderBy(static x => x.Name);
    public IEnumerable<CodeClass> InnerClasses => InnerChildElements.Values.OfType<CodeClass>().OrderBy(static x => x.Name);
    public bool ContainsMember(string name)
    {
        return InnerChildElements.ContainsKey(name);
    }
    public IEnumerable<CodeMethod> AddMethod(params CodeMethod[] methods)
    {
        if (methods == null || methods.Any(static x => x == null))
            throw new ArgumentNullException(nameof(methods));
        if (!methods.Any())
            throw new ArgumentOutOfRangeException(nameof(methods));
        return AddRange(methods);
    }

}

public class ProprietableBlockDeclaration : BlockDeclaration
{
    private readonly ConcurrentDictionary<string, CodeType> implements = new(StringComparer.OrdinalIgnoreCase);
    public void AddImplements(params CodeType[] types)
    {
        if (types == null || types.Any(x => x == null))
            throw new ArgumentNullException(nameof(types));
        EnsureElementsAreChildren(types);
        foreach (var type in types)
            implements.TryAdd(type.Name, type);
    }
    public CodeType? FindImplementByName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return implements.TryGetValue(name, out var type) ? type : null;
    }
    public void ReplaceImplementByName(string oldName, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(newName);
        var impl = FindImplementByName(oldName);
        if (impl != null)
        {
            RemoveImplements(impl);
            impl.Name = newName;
            AddImplements(impl);
        }
    }
    public void RemoveImplements(params CodeType[] types)
    {
        if (types == null || types.Any(x => x == null))
            throw new ArgumentNullException(nameof(types));
        foreach (var type in types)
            implements.TryRemove(type.Name, out var _);
    }
    public IEnumerable<CodeType> Implements => implements.Values.OrderBy(x => x.Name);
}


