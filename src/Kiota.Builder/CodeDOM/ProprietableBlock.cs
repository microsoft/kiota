﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;

/// <summary>
/// Marker interface for type testing
/// </summary>
public interface IProprietableBlock : ICodeElement {}

/// <summary>
/// Represents a block of code that can have properties and methods
/// </summary>
public abstract class ProprietableBlock<T, U> : CodeBlock<U, BlockEnd>, IDocumentedElement, IProprietableBlock where T : Enum where U : ProprietableBlockDeclaration, new()
{
    private string name;
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
    public string Description {get; set;}
    public virtual IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if(properties == null || properties.Any(x => x == null))
            throw new ArgumentNullException(nameof(properties));
        if(!properties.Any())
            throw new ArgumentOutOfRangeException(nameof(properties));
        return AddRange(properties);
    }
    public T Kind { get; set; }
    public bool IsOfKind(params T[] kinds) {
        return kinds?.Contains(Kind) ?? false;
    }
    public CodeProperty GetPropertyOfKind(CodePropertyKind kind) =>
    Properties.FirstOrDefault(x => x.IsOfKind(kind));
    public IEnumerable<CodeProperty> Properties => InnerChildElements.Values.OfType<CodeProperty>().OrderBy(x => x.Name);
    public IEnumerable<CodeMethod> Methods => InnerChildElements.Values.OfType<CodeMethod>().OrderBy(x => x.Name);
    public bool ContainsMember(string name)
    {
        return InnerChildElements.ContainsKey(name);
    }
    public IEnumerable<CodeMethod> AddMethod(params CodeMethod[] methods)
    {
        if(methods == null || methods.Any(x => x == null))
            throw new ArgumentNullException(nameof(methods));
        if(!methods.Any())
            throw new ArgumentOutOfRangeException(nameof(methods));
        return AddRange(methods);
    }
    
}

public class ProprietableBlockDeclaration : BlockDeclaration
{
    private readonly ConcurrentDictionary<string, CodeType> implements = new (StringComparer.OrdinalIgnoreCase);
    public void AddImplements(params CodeType[] types) {
        if(types == null || types.Any(x => x == null))
            throw new ArgumentNullException(nameof(types));
        EnsureElementsAreChildren(types);
        foreach(var type in types)
            implements.TryAdd(type.Name,type);
    }
    public CodeType FindImplementByName(string name) {
        if(string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        return implements.TryGetValue(name, out var type) ? type : null;
    }
    public void ReplaceImplementByName(string oldName, string newName) {
        if(string.IsNullOrEmpty(newName))
            throw new ArgumentNullException(nameof(newName));
        var impl = FindImplementByName(oldName);
        if(impl != null)
        {
            RemoveImplements(impl);
            impl.Name = newName;
            AddImplements(impl);
        }
    }
    public void RemoveImplements(params CodeType[] types) {
        if(types == null || types.Any(x => x == null))
            throw new ArgumentNullException(nameof(types));
        foreach(var type in types)
            implements.TryRemove(type.Name, out var _);
    }
    public IEnumerable<CodeType> Implements => implements.Values.OrderBy(x => x.Name);
}


