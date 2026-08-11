using System;

using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public class GoWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new GoWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new GoWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new GoWriter("./", null));
    }
}
