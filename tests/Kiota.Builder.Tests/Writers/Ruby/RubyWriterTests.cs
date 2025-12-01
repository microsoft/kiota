using System;

using Kiota.Builder.Writers.Ruby;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Ruby;

public class RubyWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new RubyWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new RubyWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new RubyWriter("./", null));
    }
}
