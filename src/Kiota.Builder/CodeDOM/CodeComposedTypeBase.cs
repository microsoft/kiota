using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;
/// <summary>
/// The base class for composed types like union or exclusion.
/// </summary>
public abstract class CodeComposedTypeBase : CodeTypeBase, IDiscriminatorInformationHolder
{
    public void AddType(params CodeType[] codeTypes)
    {
        EnsureElementsAreChildren(codeTypes);
        foreach (var codeType in codeTypes.Where(x => x != null && !Types.Contains(x)))
            types.Add(codeType);
    }
    private readonly List<CodeType> types = new();
    public IEnumerable<CodeType> Types
    {
        get => types;
    }
    private DiscriminatorInformation? _discriminatorInformation;
    /// <inheritdoc />
    public DiscriminatorInformation DiscriminatorInformation
    {
        get
        {
            if (_discriminatorInformation == null)
                DiscriminatorInformation = new DiscriminatorInformation();
            return _discriminatorInformation!;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            _discriminatorInformation = value;
        }
    }
    protected override TChildType BaseClone<TChildType>(CodeTypeBase source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is not CodeComposedTypeBase sourceComposed)
            throw new InvalidCastException($"Cannot cast {source.GetType().Name} to {nameof(CodeComposedTypeBase)}");
        base.BaseClone<TChildType>(source);
        if (sourceComposed.Types?.Any() ?? false)
            AddType(sourceComposed.Types.ToArray());
        DiscriminatorInformation = (DiscriminatorInformation)sourceComposed.DiscriminatorInformation.Clone();
        return this is TChildType casted ? casted : throw new InvalidCastException($"Cannot cast {GetType().Name} to {typeof(TChildType).Name}");
    }
    /// <summary>
    /// The target namespace if the composed type needs to be represented by a class
    /// </summary>
    public CodeNamespace? TargetNamespace
    {
        get; set;
    }
}
