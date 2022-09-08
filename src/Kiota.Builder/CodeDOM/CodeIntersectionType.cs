using System;

namespace Kiota.Builder;

/// <summary>
/// The base class for exclusion types. (one of the properties at a time)
/// </summary>
public class CodeIntersectionType : CodeComposedTypeBase, ICloneable {
    public override object Clone() {
        var value = new CodeIntersectionType{
        }.BaseClone<CodeIntersectionType>(this);
        return value;
    }
}
