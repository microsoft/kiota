using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
public abstract class ProprietableBlock<TBlockKind, TBlockDeclaration> : CodeBlock<TBlockDeclaration, BlockEnd>, IDocumentedElement, IProprietableBlock where TBlockKind : Enum where TBlockDeclaration : ProprietableBlockDeclaration, new()
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
        ArgumentNullException.ThrowIfNull(properties);

        bool hasNull = false;
        foreach (var property in properties)
        {
            if (property == null)
            {
                hasNull = true;
                break;
            }
        }

        if (hasNull)
            throw new ArgumentNullException(nameof(properties));

        if (properties.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(properties));

        return AddRange(properties);
    }
    public void RemovePropertiesOfKind(params CodePropertyKind[] kind)
    {
        if (kind == null || kind.Length == 0)
            throw new ArgumentNullException(nameof(kind));

        List<CodeProperty> propertiesToRemove = new List<CodeProperty>();
        foreach (var property in Properties)
        {
            foreach (var k in kind)
            {
                if (property.IsOfKind(k))
                {
                    propertiesToRemove.Add(property);
                    break;
                }
            }
        }

        foreach (var property in propertiesToRemove)
            RemoveChildElement(property);
    }
#nullable disable
    public TBlockKind Kind
    {
        get; set;
    }
#nullable enable
    public bool IsOfKind(params TBlockKind[] kinds)
    {
        if (kinds == null) return false;

        return Array.IndexOf(kinds, Kind) != -1;
    }
    public CodeProperty? GetPropertyOfKind(params CodePropertyKind[] kind)
    {
        if (kind == null)
            return null;

        foreach (var property in Properties)
        {
            foreach (var k in kind)
            {
                if (property.IsOfKind(k))
                    return property;
            }
        }

        return null;
    }

    public CodeProperty? GetMethodByAccessedPropertyOfKind(params CodePropertyKind[] kind)
    {
        if (kind == null)
            return null;

        foreach (var method in Methods)
        {
            if (method.AccessedProperty != null)
            {
                foreach (var k in kind)
                {
                    if (method.AccessedProperty.IsOfKind(k))
                        return method.AccessedProperty;
                }
            }
        }

        return null;
    }
    public CodeProperty? GetPropertyOfKindFromAccessorOrDirect(params CodePropertyKind[] kind) =>
        GetPropertyOfKind(kind) ?? GetMethodByAccessedPropertyOfKind(kind);

    public IEnumerable<CodeProperty> Properties
    {
        get
        {
            List<CodeProperty> properties = new List<CodeProperty>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeProperty property)
                {
                    properties.Add(property);
                }
            }
            properties.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            return properties;
        }
    }

    public IEnumerable<CodeProperty> UnorderedProperties
    {
        get
        {
            List<CodeProperty> properties = new List<CodeProperty>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeProperty property)
                {
                    properties.Add(property);
                }
            }
            return properties;
        }
    }

    public IEnumerable<CodeMethod> Methods
    {
        get
        {
            List<CodeMethod> methods = new List<CodeMethod>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeMethod method)
                {
                    methods.Add(method);
                }
            }
            methods.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            return methods;
        }
    }

    public IEnumerable<CodeMethod> UnorderedMethods
    {
        get
        {
            List<CodeMethod> methods = new List<CodeMethod>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeMethod method)
                {
                    methods.Add(method);
                }
            }
            return methods;
        }
    }

    public IEnumerable<CodeClass> InnerClasses
    {
        get
        {
            List<CodeClass> classes = new List<CodeClass>();
            foreach (var element in InnerChildElements.Values)
            {
                if (element is CodeClass classElement)
                {
                    classes.Add(classElement);
                }
            }
            classes.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            return classes;
        }
    }
    public bool ContainsMember(string name)
    {
        return InnerChildElements.ContainsKey(name);
    }
    public IEnumerable<CodeMethod> AddMethod(params CodeMethod[] methods)
    {
        if (methods == null || Array.Find(methods, static method => method == null) != null)
            throw new ArgumentNullException(nameof(methods));
        if (methods.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(methods));
        return AddRange(methods);
    }
}

public class ProprietableBlockDeclaration : BlockDeclaration
{
    private readonly ConcurrentDictionary<string, CodeType> implements = new(StringComparer.OrdinalIgnoreCase);
    public void AddImplements(params CodeType[] types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(types));
        }

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
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(types));
        }

        foreach (var type in types)
            implements.TryRemove(type.Name, out var _);
    }
    public IEnumerable<CodeType> Implements
    {
        get
        {
            List<CodeType> types = new List<CodeType>(implements.Values);
            types.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            return types;
        }
    }
}
