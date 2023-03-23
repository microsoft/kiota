using Kiota.Builder.Exceptions;

using Xunit;

namespace Kiota.Builder.Tests.Exceptions;

public class InvalidSchemaExceptionTests
{
    [Fact]
    public void Instantiates()
    {
        Assert.NotNull(new InvalidSchemaException());
    }
}
