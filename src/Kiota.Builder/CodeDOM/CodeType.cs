﻿using System;
using System.Collections.ObjectModel;

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
            GenericTypeParameterValues = new(GenericTypeParameterValues),
        }.BaseClone<CodeType>(this, TypeDefinition is null || IsExternal);
    }
    public Collection<CodeType> GenericTypeParameterValues { get; init; } = new();
}
