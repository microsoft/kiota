using System;
using System.Collections.ObjectModel;

namespace Kiota.Builder.CodeDOM;
public class CodeType : CodeTypeBase, ICloneable
{
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
            GenericTypeParameterValues = new(GenericTypeParameterValues),
        }.BaseClone<CodeType>(this);
    }
    public Collection<CodeType> GenericTypeParameterValues { get; init; } = new();
}
