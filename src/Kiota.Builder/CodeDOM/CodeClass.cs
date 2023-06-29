using System;
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
        ArgumentNullException.ThrowIfNull(properties);
        var result = new CodeProperty[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var original = StartBlock.GetOriginalPropertyDefinedFromBaseType(property.WireName);
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
            result[i] = base.AddProperty(new[] { property }).First();
        }
        return result;
    }
    private string ResolveUniquePropertyName(string name)
    {
        if (FindChildByName<CodeProperty>(name) == null)
            return name;
        if (FindChildByName<CodeProperty>(Name + name) == null)
            return Name + name;
        var i = 0;
        while (FindChildByName<CodeProperty>(Name + name + i) != null)
            i++;
        return Name + name + i;
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

    public CodeProperty? GetOriginalPropertyDefinedFromBaseType(string serializationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializationName);

        if (inherits is CodeType currentInheritsType &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
            if (currentParentClass.FindChild<CodeProperty>(x => x.WireName == serializationName) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
                return currentProperty;
            else
                return currentParentClass.StartBlock.GetOriginalPropertyDefinedFromBaseType(serializationName);
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

