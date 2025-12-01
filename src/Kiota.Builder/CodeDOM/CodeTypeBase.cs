using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public abstract class CodeTypeBase : CodeTerminal, ICloneable
{
    public enum CodeTypeCollectionKind
    {
        None,
        Array,
        Complex
    }

    /// <summary>
    /// Indicates that the type is a callback
    /// Example: ActionOf:true parameterA: (y: typeA) => void
    /// Example: ActionOf:false parameterA: typeA
    /// </summary>
    public bool ActionOf
    {
        get; set;
    }
    public bool IsNullable { get; set; } = true;
    public CodeTypeCollectionKind CollectionKind { get; set; } = CodeTypeCollectionKind.None;
    public bool IsCollection
    {
        get
        {
            return CollectionKind != CodeTypeCollectionKind.None;
        }
    }
    public bool IsArray
    {
        get
        {
            return CollectionKind == CodeTypeCollectionKind.Array;
        }
    }
    protected virtual TChildType BaseClone<TChildType>(CodeTypeBase source, bool cloneName = true) where TChildType : CodeTypeBase
    {
        ArgumentNullException.ThrowIfNull(source);
        ActionOf = source.ActionOf;
        IsNullable = source.IsNullable;
        CollectionKind = source.CollectionKind;
        if (cloneName)
            Name = source.Name;
        Parent = source.Parent;
        return this is TChildType cast ? cast : throw new InvalidOperationException($"the type {GetType()} is not compatible with the type {typeof(TChildType)}");
    }

    public abstract object Clone();

    public IEnumerable<CodeType> AllTypes
    {
        get
        {
            if (this is CodeType currentType)
                return new[] { currentType };
            if (this is CodeComposedTypeBase currentUnion)
                return currentUnion.Types;
            return Enumerable.Empty<CodeType>();
        }
    }
}
