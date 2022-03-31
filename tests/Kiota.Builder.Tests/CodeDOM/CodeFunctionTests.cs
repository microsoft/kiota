using System;
using Xunit;

namespace Kiota.Builder.Tests;

public class CodeFunctionTests {
    [Fact]
    public void Defensive() {
        var method = new CodeMethod {
            Name = "class",
        };
        Assert.Throws<ArgumentNullException>(() => new CodeFunction(null));
        Assert.Throws<InvalidOperationException>(() => new CodeFunction(method));
        method.IsStatic = true;
        var function = new CodeFunction(method);
        Assert.Equal(method, function.OriginalLocalMethod);
    }
}
