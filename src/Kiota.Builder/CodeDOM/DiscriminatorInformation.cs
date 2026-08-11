using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public class DiscriminatorInformation : CodeElement, ICloneable
{
    private ConcurrentDictionary<string, CodeType> discriminatorMappings = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets the discriminator values for the class where the key is the value as represented in the payload.
    /// </summary>
    public IOrderedEnumerable<KeyValuePair<string, CodeType>> DiscriminatorMappings
    {
        get
        {
            return (Parent is not CodeComposedTypeBase &&
                    Parent?.GetImmediateParentOfType<CodeClass>() is CodeClass parentClass ?
                        discriminatorMappings.Where(x => x.Value is not CodeType currentType || currentType.TypeDefinition != parentClass) :
                        discriminatorMappings)
                    .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase);
        }
    }
    /// <summary>
    /// Gets/Sets the name of the property to use for discrimination during deserialization.
    /// </summary>
    public string DiscriminatorPropertyName
    {
        get; set;
    } = string.Empty;

    public void AddDiscriminatorMapping(string key, CodeType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(key);
        discriminatorMappings.TryAdd(key, type);
    }

    public CodeTypeBase? GetDiscriminatorMappingValue(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (discriminatorMappings.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public void RemoveDiscriminatorMapping(params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
            discriminatorMappings.TryRemove(key, out var _);
    }

    public void RemoveDiscriminatorMapping(CodeClass classToRemove)
    {
        ArgumentNullException.ThrowIfNull(classToRemove);
        var keyToRemove = discriminatorMappings.FirstOrDefault(x => x.Value is CodeType currentType && currentType.TypeDefinition == classToRemove).Key;
        if (!string.IsNullOrEmpty(keyToRemove))
            discriminatorMappings.TryRemove(keyToRemove, out var _);
    }

    public object Clone()
    {
        return new DiscriminatorInformation
        {
            DiscriminatorPropertyName = DiscriminatorPropertyName,
            discriminatorMappings = new(discriminatorMappings, StringComparer.OrdinalIgnoreCase),
            Parent = Parent,
            Name = Name,
        };
    }
    public bool HasBasicDiscriminatorInformation => !string.IsNullOrEmpty(DiscriminatorPropertyName) && !discriminatorMappings.IsEmpty;
    public bool ShouldWriteDiscriminatorForInheritedType => HasBasicDiscriminatorInformation && IsComplexType;
    public bool ShouldWriteDiscriminatorForUnionType => IsUnionType; // if union of scalar types, then we don't always get discriminator information
    public bool ShouldWriteDiscriminatorForIntersectionType => IsIntersectionType; // if intersection of scalar types, then we don't always get discriminator information
    public bool ShouldWriteParseNodeCheck => ShouldWriteDiscriminatorForInheritedType || ShouldWriteDiscriminatorForUnionType || ShouldWriteDiscriminatorForIntersectionType;
    private bool IsUnionType => Is<CodeUnionType>();
    private bool IsIntersectionType => Is<CodeIntersectionType>();
    private bool IsComplexType => Parent is CodeClass currentClass && currentClass.OriginalComposedType is null ||
                                Parent is CodeMethod parentMethod && parentMethod.Parent is CodeFunction currentFunction && currentFunction.OriginalMethodParentClass?.OriginalComposedType is null; //static factories outside of classes (TS/Go)
    private bool Is<T>() where T : CodeComposedTypeBase
    {
        return Parent is CodeClass currentClass && currentClass.OriginalComposedType is T ||
        Parent is T;
    }
}
