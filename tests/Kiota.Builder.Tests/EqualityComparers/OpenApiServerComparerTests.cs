using Kiota.Builder.EqualityComparers;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests.EqualityComparers;

public class OpenApiServerComparerTests
{
    [Fact]
    public void ProtocolAgnostic()
    {
        var comparer = new OpenApiServerComparer();
        var s1 = new OpenApiServer { Url = "http://localhost" };
        var s2 = new OpenApiServer { Url = "https://localhost" };

        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs://localhost" };
        s2 = new OpenApiServer { Url = "http://localhost" };
        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs://" };
        s2 = new OpenApiServer { Url = "http://" };
        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs:/" };
        s2 = new OpenApiServer { Url = "http:/" };
        Assert.NotEqual(s1, s2, comparer);
    }
}
