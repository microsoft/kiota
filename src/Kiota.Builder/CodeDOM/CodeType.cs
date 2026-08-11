using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public class CodeType : CodeTypeBase, ICloneable
{
    public override string Name
    {
        get => IsExternal || TypeDefinition is null ? base.Name : TypeDefinition.Name;
        set
        {
            if (!IsExternal && TypeDefinition is not null)
                TypeDefinition.Name = value;
            else
                base.Name = value;
        }
    }
    public CodeElement? TypeDefinition
    {
        get;
        set;
    }
    public bool IsExternal
    {
        get; set;
    }

    public override object Clone()
    {
        return new CodeType
        {
            TypeDefinition = TypeDefinition, // not cloning the type definition as it's a code element that lives in the tree and we don't want to fork the tree
            IsExternal = IsExternal,
            // Clone the list so that modifications on cloned objects' property are localized
            // e.g. var y = x.Clone(); var z = y.Clone(); y.GenericTypeParameterValues.Add(value);
            // shouldn't modify x.GenericTypeParameterValues or z.GenericTypeParameterValues
            genericTypeParameterValues = [.. genericTypeParameterValues],
        }.BaseClone<CodeType>(this, TypeDefinition is null || IsExternal);
    }
    public IEnumerable<CodeType> GenericTypeParameterValues
    {
        get => genericTypeParameterValues;
        init => AddGenericTypeParameterValue([.. value]);
    }
    private Collection<CodeType> genericTypeParameterValues = [];
    public void AddGenericTypeParameterValue(params CodeType[] types)
    {
        if (types is null) return;
        EnsureElementsAreChildren(types);
        foreach (var type in types)
            genericTypeParameterValues.Add(type);
    }
}
