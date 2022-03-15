using System;
using System.Linq;

namespace Kiota.Builder;

public abstract class CodeTerminalWithKind<T> : CodeTerminal where T : Enum {
    public T Kind { get; set; }
    public bool IsOfKind(params T[] kinds) {
        return kinds?.Contains(Kind) ?? false;
    }
}
