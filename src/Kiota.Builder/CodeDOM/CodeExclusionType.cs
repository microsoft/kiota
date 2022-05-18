using System;

namespace Kiota.Builder;

/// <summary>
/// The base class for exclusion types. (one of the properties at a time)
/// </summary>
public class CodeExclusionType : CodeComposedTypeBase, ICloneable {
    public override object Clone() {
        var value = new CodeExclusionType{
        }.BaseClone<CodeExclusionType>(this);
        return value;
    }
}
