
using System;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.SearchProviders.GitHub.Authentication;

public class PatAuthenticationProviderTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => new PatAuthenticationProvider(null, "foo", new string[] { "foo" }, new Mock<ILogger>().Object, new Mock<ITokenStorageService>().Object));
        Assert.Throws<ArgumentNullException>(() => new PatAuthenticationProvider("foo", null, new string[] { "foo" }, new Mock<ILogger>().Object, new Mock<ITokenStorageService>().Object));
        Assert.Throws<ArgumentNullException>(() => new PatAuthenticationProvider("foo", "foo", new string[] { "foo" }, null, new Mock<ITokenStorageService>().Object));
        Assert.Throws<ArgumentNullException>(() => new PatAuthenticationProvider("foo", "foo", new string[] { "foo" }, new Mock<ILogger>().Object, null));
    }

}
