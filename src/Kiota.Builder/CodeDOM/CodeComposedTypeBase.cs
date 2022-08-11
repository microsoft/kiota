using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;
/// <summary>
/// The base class for composed types like union or exclusion.
/// </summary>
public abstract class CodeComposedTypeBase : CodeTypeBase, IDiscriminatorInformationHolder {
    public void AddType(params CodeType[] codeTypes) {
        EnsureElementsAreChildren(codeTypes);
        foreach(var codeType in codeTypes.Where(x => x != null && !Types.Contains(x)))
            types.Add(codeType);
    }
    private readonly List<CodeType> types = new ();
    public IEnumerable<CodeType> Types { get => types; }
    private DiscriminatorInformation _discriminatorInformation;
    /// <inheritdoc />
    public DiscriminatorInformation DiscriminatorInformation {
        get {
            if (_discriminatorInformation == null)
                DiscriminatorInformation = new DiscriminatorInformation();
            return _discriminatorInformation;
        } 
        set {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            EnsureElementsAreChildren(value);
            _discriminatorInformation = value;
        }
    }
    protected override ChildType BaseClone<ChildType>(CodeTypeBase source) {
        if (source is not CodeComposedTypeBase sourceComposed)
            throw new InvalidCastException($"Cannot cast {source.GetType().Name} to {nameof(CodeComposedTypeBase)}");
        base.BaseClone<ChildType>(source);
        if(sourceComposed.Types?.Any() ?? false)
            AddType(sourceComposed.Types.ToArray());
        DiscriminatorInformation = sourceComposed.DiscriminatorInformation?.Clone() as DiscriminatorInformation;
        return this as ChildType;
    }
    /// <summary>
    /// The target namespace if the composed type needs to be represented by a class
    /// </summary>
    public CodeNamespace TargetNamespace { get; set; }
}
