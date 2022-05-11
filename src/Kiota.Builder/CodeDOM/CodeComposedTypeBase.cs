using System;
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
    protected override ChildType BaseClone<ChildType>(CodeTypeBase source) {
        if (source is not CodeComposedTypeBase sourceComposed)
            throw new InvalidCastException($"Cannot cast {source.GetType().Name} to {nameof(CodeComposedTypeBase)}");
        base.BaseClone<ChildType>(source);
        if(sourceComposed.Types?.Any() ?? false)
            AddType(sourceComposed.Types.ToArray());
        return this as ChildType;
    }
}
