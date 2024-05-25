using System;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// Abstract element of some piece of source code to be generated
/// </summary>
public abstract class CodeElement : ICodeElement
{
    public CodeElement? Parent
    {
        get; set;
    }
    public int GetNamespaceDepth(int currentDepth = 0)
    {
        return this switch
        {
            _ when Parent is null => currentDepth,
            CodeNamespace ns => ns.Parent?.GetNamespaceDepth(1 + currentDepth) ?? currentDepth,
            _ => Parent.GetNamespaceDepth(currentDepth),
        };
    }
    public virtual IEnumerable<CodeElement> GetChildElements(bool innerOnly = false) => [];

    public virtual string Name
    {
        get; set;
    } = string.Empty;

    protected void EnsureElementsAreChildren(params ICodeElement?[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        foreach (var element in elements)
        {
            if (element != null && (element.Parent == null || element.Parent != this))
            {
                element.Parent = this;
            }
        }
    }


    public T GetImmediateParentOfType<T>(CodeElement? item = null)
    {
        if (item == null)
            return GetImmediateParentOfType<T>(this);
        if (item is T p)
            return p;
        if (item.Parent == null)
            throw new InvalidOperationException($"item {item.Name} of type {item.GetType()} does not have a parent");
        if (item.Parent is T p2)
            return p2;
        return GetImmediateParentOfType<T>(item.Parent);
    }
    public bool IsChildOf(CodeElement codeElement, bool immediateOnly = false)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (Parent == codeElement) return true;
        if (immediateOnly || Parent == null) return false;
        return Parent.IsChildOf(codeElement, immediateOnly);
    }
}
