
using System;
using Xunit;

namespace Kiota.Builder.Exceptions.Tests;

public class InvalidSchemaExceptionTests {
    [Fact]
    public void Instantiates() {
        Assert.NotNull(new InvalidSchemaException());
        Assert.NotNull(new InvalidSchemaException("message"));
        Assert.NotNull(new InvalidSchemaException("message", new InvalidOperationException()));
    }
}
