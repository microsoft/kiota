using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;
/// <summary>
/// The base class for composed types like union or exclusion.
/// </summary>
public abstract class CodeComposedTypeBase : CodeTypeBase {
    public void AddType(params CodeType[] codeTypes) {
        EnsureElementsAreChildren(codeTypes);
        foreach(var codeType in codeTypes.Where(x => x != null && !Types.Contains(x)))
            types.Add(codeType);
    }
    private readonly List<CodeType> types = new ();
    public IEnumerable<CodeType> Types { get => types; }
    public string DiscriminatorPropertyName { get; set; }
    private ConcurrentDictionary<string, CodeTypeBase> discriminatorMappings = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets/Sets the discriminator values for the class where the key is the value as represented in the payload.
    /// </summary>
    public IOrderedEnumerable<KeyValuePair<string, CodeTypeBase>> DiscriminatorMappings
    {
        get
        {
            return discriminatorMappings.OrderBy(static x => x.Key);
        }
    }
    public void AddDiscriminatorMapping(string key, CodeTypeBase type)
    {
        if(type == null) throw new ArgumentNullException(nameof(type));
        if(string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        discriminatorMappings.TryAdd(key, type);
    }
    protected override ChildType BaseClone<ChildType>(CodeTypeBase source) {
        if (source is not CodeComposedTypeBase sourceComposed)
            throw new InvalidCastException($"Cannot cast {source.GetType().Name} to {nameof(CodeComposedTypeBase)}");
        base.BaseClone<ChildType>(source);
        if(sourceComposed.Types?.Any() ?? false)
            AddType(sourceComposed.Types.ToArray());
        DiscriminatorPropertyName = sourceComposed.DiscriminatorPropertyName;
        if(sourceComposed.DiscriminatorMappings?.Any() ?? false)
            discriminatorMappings = new(sourceComposed.DiscriminatorMappings.ToDictionary(static x => x.Key, static x => x.Value.Clone() as CodeTypeBase), StringComparer.OrdinalIgnoreCase);
        return this as ChildType;
    }
}
