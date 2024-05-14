using System;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The base class for union types. (anyOf multiple properties at a time)
/// </summary>
public class CodeUnionType : CodeComposedTypeBase, ICloneable
{
    public override string FullName
    {
        get => Name;
    }
    public override object Clone()
    {
        return new CodeUnionType().BaseClone<CodeUnionType>(this);
    }
}
