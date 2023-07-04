using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public enum CodeClassKind
{
    Custom,
    RequestBuilder,
    Model,
    QueryParameters,
    /// <summary>
    /// A single parameter to be provided by the SDK user which will contain query parameters, request body, options, etc.
    /// Only used for languages that do not support overloads or optional parameters like go.
    /// </summary>
    ParameterSet,
    /// <summary>
    /// A class used as a placeholder for the barrel file.
    /// </summary>
    BarrelInitializer,
    /// <summary>
    /// Configuration for the request to be sent with the headers, query parameters, and middleware options
    /// </summary>
    RequestConfiguration,
}
/// <summary>
/// CodeClass represents an instance of a Class to be generated in source code
/// </summary>
public class CodeClass : ProprietableBlock<CodeClassKind, ClassDeclaration>, ITypeDefinition, IDiscriminatorInformationHolder, IDeprecableElement
{
    protected ConcurrentDictionary<string, CodeProperty> PropertiesByWireName { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsErrorDefinition
    {
        get; set;
    }

    /// <summary>
    /// Original composed type this class was generated for.
    /// </summary>
    public CodeComposedTypeBase? OriginalComposedType
    {
        get; set;
    }
    public CodeIndexer? Indexer
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (InnerChildElements.Values.OfType<CodeIndexer>().Any() || InnerChildElements.Values.OfType<CodeMethod>().Any(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)))
            {
                if (Indexer is CodeIndexer existingIndexer)
                {
                    RemoveChildElement(existingIndexer);
                    AddRange(CodeMethod.FromIndexer(existingIndexer, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), true));
                }
                AddRange(CodeMethod.FromIndexer(value, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), false));
            }
            else
                AddRange(value);
        }
        get => InnerChildElements.Values.OfType<CodeIndexer>().FirstOrDefault();
    }
    public override IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if (properties == null || properties.Any(static x => x == null))
            throw new ArgumentNullException(nameof(properties));
        if (!properties.Any())
            throw new ArgumentOutOfRangeException(nameof(properties));

        return properties.Select(property =>
        {
            if (property.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.QueryParameter))
            {
                var original = GetOriginalPropertyDefinedFromBaseType(property.WireName);
                if (original == null)
                {
                    var uniquePropertyName = ResolveUniquePropertyName(property.Name);
                    if (uniquePropertyName != property.Name && String.IsNullOrEmpty(property.SerializationName))
                        property.SerializationName = property.Name;
                    property.Name = uniquePropertyName;
                }
                else
                {
                    // the property already exists in a parent type, use its name
                    property.Name = original.Name;
                    property.SerializationName = original.SerializationName;
                    property.OriginalPropertyFromBaseType = original!;
                }
            }
            CodeProperty result = base.AddProperty(new[] { property }).First();
            return PropertiesByWireName.GetOrAdd(result.WireName, result);
        }).ToArray();
    }
    public override void RenameChildElement(string oldName, string newName)
    {
        if (InnerChildElements.TryRemove(oldName, out var element))
        {
            if (element is CodeProperty removedProperty)
            {
                PropertiesByWireName.TryRemove(removedProperty.WireName, out _);
            }
            element.Name = newName;
            InnerChildElements.TryAdd(newName, element);
            if (element is CodeProperty propertyToAdd)
            {
                PropertiesByWireName.TryAdd(propertyToAdd.WireName, propertyToAdd);
            }
        }
    }
    public override void RemoveChildElementByName(params string[] names)
    {
        if (names == null) return;

        foreach (var name in names)
        {
            if (InnerChildElements.TryRemove(name, out var removedElement) && removedElement is CodeProperty removedProperty)
            {
                PropertiesByWireName.TryRemove(removedProperty.WireName, out _);
            }
        }
    }
    private string ResolveUniquePropertyName(string name)
    {
        if (FindPropertyByNameInTypeHierarchy(name) == null)
            return name;
        // the CodeClass.Name is not very useful as prefix for the property name, so keep the original name and add a number
        var nameWithTypeName = Kind == CodeClassKind.QueryParameters ? name : Name + name.ToFirstCharacterUpperCase();
        if (Kind != CodeClassKind.QueryParameters)
        {
            if (FindPropertyByNameInTypeHierarchy(nameWithTypeName) == null)
                return nameWithTypeName;
        }
        var i = 0;
        while (FindPropertyByNameInTypeHierarchy(nameWithTypeName + i) != null)
            i++;
        return nameWithTypeName + i;
    }
    private CodeProperty? FindPropertyByNameInTypeHierarchy(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (FindChildByName<CodeProperty>(propertyName, findInChildElements: false) is CodeProperty result)
        {
            return result;
        }
        if (ParentClass is CodeClass currentParentClass)
        {
            return currentParentClass.FindPropertyByNameInTypeHierarchy(propertyName);
        }
        return default;
    }
    private CodeProperty? GetOriginalPropertyDefinedFromBaseType(string serializationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializationName);

        if (ParentClass is CodeClass currentParentClass)
            if (currentParentClass.FindPropertyByWireName(serializationName) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
                return currentProperty;
            else
                return currentParentClass.GetOriginalPropertyDefinedFromBaseType(serializationName);
        return default;
    }
    private CodeProperty? FindPropertyByWireName(string wireName)
    {
        if (!PropertiesByWireName.Any())
            return default;

        if (PropertiesByWireName.TryGetValue(wireName, out var result))
            return result;
        return InnerChildElements.Values.OfType<CodeClass>().Select(x => x.FindPropertyByWireName(wireName)).OfType<CodeProperty>().FirstOrDefault();
    }
    public bool ContainsPropertyWithWireName(string wireName)
    {
        return PropertiesByWireName.ContainsKey(wireName);
    }
    public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
    {
        if (codeClasses == null || codeClasses.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if (!codeClasses.Any())
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    public IEnumerable<CodeInterface> AddInnerInterface(params CodeInterface[] codeInterfaces)
    {
        if (codeInterfaces == null || codeInterfaces.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeInterfaces));
        if (!codeInterfaces.Any())
            throw new ArgumentOutOfRangeException(nameof(codeInterfaces));
        return AddRange(codeInterfaces);
    }
    public CodeClass? ParentClass => StartBlock.Inherits?.TypeDefinition as CodeClass;
    public bool DerivesFrom(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        var parent = ParentClass;
        if (parent == null)
            return false;
        if (parent == codeClass)
            return true;
        return parent.DerivesFrom(codeClass);
    }
    public Collection<CodeClass> GetInheritanceTree(bool currentNamespaceOnly = false, bool includeCurrentClass = true)
    {
        var parentClass = ParentClass;
        if (parentClass == null || (currentNamespaceOnly && parentClass.GetImmediateParentOfType<CodeNamespace>() != GetImmediateParentOfType<CodeNamespace>()))
            if (includeCurrentClass)
                return new() { this };
            else
                return new();
        var result = parentClass.GetInheritanceTree(currentNamespaceOnly);
        result.Add(this);
        return result;
    }
    public CodeClass? GetGreatestGrandparent(CodeClass? startClassToSkip = default)
    {
        var parentClass = ParentClass;
        if (parentClass == null)
            return startClassToSkip != null && startClassToSkip == this ? null : this;
        // we don't want to return the current class if this is the start node in the inheritance tree and doesn't have parent
        return parentClass.GetGreatestGrandparent(startClassToSkip);
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

    public DeprecationInformation? Deprecation
    {
        get; set;
    }
}
public class ClassDeclaration : ProprietableBlockDeclaration
{
    private CodeType? inherits;
    public CodeType? Inherits
    {
        get => inherits; set
        {
            EnsureElementsAreChildren(value);
            inherits = value;
        }
    }
    public bool InheritsFrom(CodeClass candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (inherits is CodeType currentInheritsType &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
            if (currentParentClass == candidate)
                return true;
            else
                return currentParentClass.StartBlock.InheritsFrom(candidate);
        return false;
    }
}

