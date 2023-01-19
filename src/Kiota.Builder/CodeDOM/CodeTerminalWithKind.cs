using System;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public abstract class CodeTerminalWithKind<T> : CodeTerminal where T : Enum {
#nullable disable
    public T Kind { get; set; }
#nullable enable
    public bool IsOfKind(params T[] kinds) {
        return kinds?.Contains(Kind) ?? false;
    }
}
