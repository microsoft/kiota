using System;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The base class for union types. (anyOf multiple properties at a time)
/// </summary>
public class CodeUnionType : CodeComposedTypeBase, ICloneable, IDeprecableElement
{
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
    public override object Clone()
    {
        var value = new CodeUnionType().BaseClone<CodeUnionType>(this);
        value.Deprecation = Deprecation;
        return value;
    }
}
