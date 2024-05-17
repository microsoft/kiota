using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
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
    private readonly ConcurrentDictionary<string, CodeProperty> PropertiesByWireName = new(StringComparer.OrdinalIgnoreCase);
    public bool IsErrorDefinition
    {
        get; set;
    }

    /// <summary>
    /// Original composed type this class was generated for.
    /// </summary>
    [JsonIgnore]
    public CodeComposedTypeBase? OriginalComposedType
    {
        get; set;
    }
    public string GetComponentSchemaName(CodeNamespace modelsNamespace)
    {
        if (Kind is not CodeClassKind.Model ||
                Parent is not CodeNamespace parentNamespace ||
                !parentNamespace.IsChildOf(modelsNamespace))
            return string.Empty;
        return $"{parentNamespace.Name[(modelsNamespace.Name.Length + 1)..]}.{Name}";
    }
    public CodeIndexer? Indexer => InnerChildElements.Values.OfType<CodeIndexer>().FirstOrDefault(static x => !x.IsLegacyIndexer);
    public void AddIndexer(params CodeIndexer[] indexers)
    {
        if (indexers == null || Array.Exists(indexers, static x => x == null))
            throw new ArgumentNullException(nameof(indexers));
        if (indexers.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(indexers));

        foreach (var value in indexers)
        {
            var existingIndexers = InnerChildElements.Values.OfType<CodeIndexer>().ToArray();
            if (Array.Exists(existingIndexers, x => !x.IndexParameter.Name.Equals(value.IndexParameter.Name, StringComparison.OrdinalIgnoreCase)) ||
                    InnerChildElements.Values.OfType<CodeMethod>().Any(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)))
            {
                foreach (var existingIndexer in existingIndexers)
                {
                    RemoveChildElement(existingIndexer);
                    AddRange(CodeMethod.FromIndexer(existingIndexer, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), true));
                }
                AddRange(CodeMethod.FromIndexer(value, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), false));
            }
            else
                AddRange(value);
        }
    }
    public override IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if (properties == null || properties.Any(static x => x == null))
            throw new ArgumentNullException(nameof(properties));
        if (properties.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(properties));

        return properties.Select(property =>
        {
            if (property.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.QueryParameter))
            {
                if (GetOriginalPropertyDefinedFromBaseType(property.WireName) is CodeProperty original)
                {
                    // the property already exists in a parent type, use its name
                    property.Name = original.Name;
                    property.SerializationName = original.SerializationName;
                    property.OriginalPropertyFromBaseType = original;
                }
                else
                {
                    var uniquePropertyName = ResolveUniquePropertyName(property.Name);
                    if (!uniquePropertyName.Equals(property.Name, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(property.SerializationName))
                        property.SerializationName = property.Name;
                    property.Name = uniquePropertyName;
                }
            }
            var result = base.AddProperty(property).First();
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
            AddRange(element);
            if (element is CodeProperty propertyToAdd)
            {
                PropertiesByWireName.TryAdd(propertyToAdd.WireName, propertyToAdd);
            }
        }
        else throw new InvalidOperationException($"The element {oldName} could not be found in the class {Name}");
    }
    public override void RemoveChildElementByName(params string[] names)
    {
        if (names == null) return;

        foreach (var name in names)
        {
            if (InnerChildElements.TryRemove(name, out var removedElement))
            {
                if (removedElement is CodeProperty removedProperty)
                    PropertiesByWireName.TryRemove(removedProperty.WireName, out _);
            }
            else throw new InvalidOperationException($"The element {name} could not be found in the class {Name}");
        }
    }
    public void RemoveMethodByKinds(params CodeMethodKind[] kinds)
    {
        RemoveChildElementByName(InnerChildElements.Where(x => x.Value is CodeMethod method && method.IsOfKind(kinds)).Select(static x => x.Key).ToArray());
    }
    private string ResolveUniquePropertyName(string name)
    {
        if (FindPropertyByNameInTypeHierarchy(name) == null)
            return name;
        // the CodeClass.Name is not very useful as prefix for the property name, so keep the original name and add a number
        var nameWithTypeName = Kind == CodeClassKind.QueryParameters ? name : Name + name.ToFirstCharacterUpperCase();
        if (Kind != CodeClassKind.QueryParameters && FindPropertyByNameInTypeHierarchy(nameWithTypeName) == null)
            return nameWithTypeName;
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
        if (BaseClass is CodeClass currentParentClass)
        {
            return currentParentClass.FindPropertyByNameInTypeHierarchy(propertyName);
        }
        return default;
    }
    private CodeProperty? GetOriginalPropertyDefinedFromBaseType(string serializationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializationName);

        if (BaseClass is CodeClass currentParentClass)
            if (currentParentClass.FindPropertyByWireName(serializationName) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType && currentProperty.Kind is not CodePropertyKind.AdditionalData or CodePropertyKind.BackingStore)
                return currentProperty;
            else
                return currentParentClass.GetOriginalPropertyDefinedFromBaseType(serializationName);
        return default;
    }
    private CodeProperty? FindPropertyByWireName(string wireName)
    {
        return PropertiesByWireName.TryGetValue(wireName, out var result) ? result : default;
    }
    public bool ContainsPropertyWithWireName(string wireName)
    {
        return PropertiesByWireName.ContainsKey(wireName);
    }
    public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
    {
        if (codeClasses == null || codeClasses.Any(static x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if (codeClasses.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    public IEnumerable<CodeInterface> AddInnerInterface(params CodeInterface[] codeInterfaces)
    {
        if (codeInterfaces == null || codeInterfaces.Any(static x => x == null))
            throw new ArgumentNullException(nameof(codeInterfaces));
        if (codeInterfaces.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(codeInterfaces));
        return AddRange(codeInterfaces);
    }
    public CodeClass? BaseClass => StartBlock.Inherits?.TypeDefinition as CodeClass;
    /// <summary>
    /// The interface associated with this class, if any.
    /// </summary>
    [JsonIgnore]
    public CodeInterface? AssociatedInterface
    {
        get; set;
    }
    public bool DerivesFrom(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        var parent = BaseClass;
        if (parent == null)
            return false;
        if (parent == codeClass)
            return true;
        return parent.DerivesFrom(codeClass);
    }
    public Collection<CodeClass> GetInheritanceTree(bool currentNamespaceOnly = false, bool includeCurrentClass = true)
    {
        var parentClass = BaseClass;
        if (parentClass == null || (currentNamespaceOnly && parentClass.GetImmediateParentOfType<CodeNamespace>() != GetImmediateParentOfType<CodeNamespace>()))
            if (includeCurrentClass)
                return [this];
            else
                return [];
        var result = parentClass.GetInheritanceTree(currentNamespaceOnly);
        result.Add(this);
        return result;
    }
    public CodeClass? GetGreatestGrandparent(CodeClass? startClassToSkip = default)
    {
        var parentClass = BaseClass;
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
    [JsonIgnore]
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
            if (value != null && !value.IsExternal && Parent is CodeClass codeClass && codeClass.Properties.Any())
            {
                throw new InvalidOperationException("Cannot change the inherits-property of an already populated type");
            }

            EnsureElementsAreChildren(value);
            inherits = value;
        }
    }
    public CodeProperty? GetOriginalPropertyDefinedFromBaseType(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (inherits is CodeType currentInheritsType &&
            !inherits.IsExternal &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
            if (currentParentClass.FindChildByName<CodeProperty>(propertyName, false) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
                return currentProperty;
            else
                return currentParentClass.StartBlock.GetOriginalPropertyDefinedFromBaseType(propertyName);
        return default;
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

