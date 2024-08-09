using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.CodeDOM;
/// <summary>
/// The base class for composed types like union or exclusion.
/// </summary>
public abstract class CodeComposedTypeBase : CodeTypeBase, IDiscriminatorInformationHolder, IDeprecableElement
{
    private static string NormalizeKey(CodeType codeType) => $"{codeType.Name}_{codeType.CollectionKind}";
    public void AddType(params CodeType[] codeTypes)
    {
        ArgumentNullException.ThrowIfNull(codeTypes);
        if (Array.Exists(codeTypes, static x => x == null))
            throw new ArgumentNullException(nameof(codeTypes), "One of the provided types was null");
        EnsureElementsAreChildren(codeTypes);
        foreach (var codeType in codeTypes)
            if (!types.TryAdd(NormalizeKey(codeType), codeType))
                throw new InvalidOperationException($"The type {codeType.Name} was already added");
    }
    public bool ContainsType(CodeType codeType)
    {
        ArgumentNullException.ThrowIfNull(codeType);
        return types.ContainsKey(NormalizeKey(codeType));
    }
    private readonly ConcurrentDictionary<string, CodeType> types = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<CodeType> Types
    {
        get => types.Values.OrderBy(NormalizeKey, StringComparer.OrdinalIgnoreCase);
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
    protected override TChildType BaseClone<TChildType>(CodeTypeBase source, bool cloneName = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is not CodeComposedTypeBase sourceComposed)
            throw new InvalidCastException($"Cannot cast {source.GetType().Name} to {nameof(CodeComposedTypeBase)}");
        base.BaseClone<TChildType>(source, cloneName);
        if (sourceComposed.Types?.Any() ?? false)
            AddType(sourceComposed.Types.ToArray());
        DiscriminatorInformation = (DiscriminatorInformation)sourceComposed.DiscriminatorInformation.Clone();
        Deprecation = sourceComposed.Deprecation;
        return this is TChildType casted ? casted : throw new InvalidCastException($"Cannot cast {GetType().Name} to {typeof(TChildType).Name}");
    }
    /// <summary>
    /// The target namespace if the composed type needs to be represented by a class
    /// </summary>
    public CodeNamespace? TargetNamespace
    {
        get; set;
    }
    public DeprecationInformation? Deprecation
    {
        get;
        set;
    }
    public bool IsComposedOfPrimitives() => Types.All(x => IsPrimitiveType(GetTypescriptTypeString(x, this)));
    public bool IsComposedOfObjectsAndPrimitives()
    {
        // Count the number of primitives in Types
        int primitiveCount = Types.Count(x => IsPrimitiveType(GetTypescriptTypeString(x, this)));

        // If the number of primitives is less than the total count, it means the rest are objects
        return primitiveCount > 0 && primitiveCount < Types.Count();
    }

    public IEnumerable<CodeType> GetPrimitiveTypes()
    {
        // Return only the primitive types from the Types collection
        return Types.Where(x => IsPrimitiveType(GetTypescriptTypeString(x, this)));
    }

    public IEnumerable<CodeType> GetNonPrimitiveTypes()
    {
        // Return only the non primitive types from the Types collection
        return Types.Where(x => !IsPrimitiveType(GetTypescriptTypeString(x, this)));
    }
}
