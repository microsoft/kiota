﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder;

public enum CodeClassKind {
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
public class CodeClass : ProprietableBlock<CodeClassKind, ClassDeclaration>, ITypeDefinition
{
    public bool IsErrorDefinition { get; set; }
    public void SetIndexer(CodeIndexer indexer)
    {
        if(indexer == null)
            throw new ArgumentNullException(nameof(indexer));
        if(InnerChildElements.Values.OfType<CodeIndexer>().Any() || InnerChildElements.Values.OfType<CodeMethod>().Any(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility))) {
            var existingIndexer = InnerChildElements.Values.OfType<CodeIndexer>().FirstOrDefault();
            if(existingIndexer != null) {
                RemoveChildElement(existingIndexer);
                AddRange(CodeMethod.FromIndexer(existingIndexer, this, $"By{existingIndexer.SerializationName.CleanupSymbolName().ToFirstCharacterUpperCase()}", true));
            }
            AddRange(CodeMethod.FromIndexer(indexer, this, $"By{indexer.SerializationName.CleanupSymbolName().ToFirstCharacterUpperCase()}", false));
        } else
            AddRange(indexer);
    }
    public override IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties) {
        var result = base.AddProperty(properties);
        foreach(var addedPropertyTuple in result.Select(x => new Tuple<CodeProperty, CodeProperty>(x, StartBlock.GetOriginalPropertyDefinedFromBaseType(x.Name)))
                                        .Where(static x => x.Item2 != null))
            addedPropertyTuple.Item1.OriginalPropertyFromBaseType = addedPropertyTuple.Item2;

        return result;
    }
    public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
    {
        if(codeClasses == null || codeClasses.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if(!codeClasses.Any())
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    public IEnumerable<CodeInterface> AddInnerInterface(params CodeInterface[] codeInterfaces)
    {
        if(codeInterfaces == null || codeInterfaces.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeInterfaces));
        if(!codeInterfaces.Any())
            throw new ArgumentOutOfRangeException(nameof(codeInterfaces));
        return AddRange(codeInterfaces);
    }
    public CodeClass GetParentClass() {
        return StartBlock.Inherits?.TypeDefinition as CodeClass;
    }
    
    public CodeClass GetGreatestGrandparent(CodeClass startClassToSkip = null) {
        var parentClass = GetParentClass();
        if(parentClass == null)
            return startClassToSkip != null && startClassToSkip == this ? null : this;
        // we don't want to return the current class if this is the start node in the inheritance tree and doesn't have parent
        else
            return parentClass.GetGreatestGrandparent(startClassToSkip);
    }
}
public class ClassDeclaration : ProprietableBlockDeclaration
{
    private CodeType inherits;
    public CodeType Inherits { get => inherits; set {
        EnsureElementsAreChildren(value);
        inherits = value;
    } }

    public CodeProperty GetOriginalPropertyDefinedFromBaseType(string propertyName) {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        if (inherits is CodeType currentInheritsType &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
            if (currentParentClass.FindChildByName<CodeProperty>(propertyName) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
                return currentProperty;
            else
                return currentParentClass.StartBlock.GetOriginalPropertyDefinedFromBaseType(propertyName);
        else
            return default;
    }

    public bool InheritsFrom(CodeClass candidate) {
        ArgumentNullException.ThrowIfNull(candidate, nameof(candidate));

        if (inherits is CodeType currentInheritsType &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
            if (currentParentClass == candidate)
                return true;
            else
                return currentParentClass.StartBlock.InheritsFrom(candidate);
        else
            return false;
    }
}

