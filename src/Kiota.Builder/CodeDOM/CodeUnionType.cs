using System;

namespace Kiota.Builder;

/// <summary>
/// The base class for union types. (anyOf multiple properties at a time)
/// </summary>
public class CodeUnionType : CodeComposedTypeBase, ICloneable {
    public override object Clone() {
        var value = new CodeUnionType{
        }.BaseClone<CodeUnionType>(this);
        return value;
    }
}
