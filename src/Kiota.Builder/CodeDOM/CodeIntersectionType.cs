using System;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The base class for exclusion types. (one of the properties at a time)
/// </summary>
public class CodeIntersectionType : CodeComposedTypeBase, ICloneable, IDeprecableElement
{
    public DeprecationInformation? Deprecation
    {
        get; set;
    }

    public override object Clone()
    {
        var value = new CodeIntersectionType().BaseClone<CodeIntersectionType>(this);
        value.Deprecation = Deprecation;
        return value;
    }
}
