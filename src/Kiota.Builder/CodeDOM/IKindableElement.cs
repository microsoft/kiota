using System;

namespace Kiota.Builder.CodeDOM;

public interface IKindableElement<TKind> where TKind : Enum
{
    TKind Kind
    {
        get; set;
    }
    bool IsOfKind(params TKind[] kinds);
}
