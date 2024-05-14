using System;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The base class for exclusion types. (one of the properties at a time)
/// </summary>
public class CodeIntersectionType : CodeComposedTypeBase, ICloneable
{
    public override string FullName
    {
        get => Name;
    }
    public override object Clone()
    {
        return new CodeIntersectionType().BaseClone<CodeIntersectionType>(this);
    }
}
