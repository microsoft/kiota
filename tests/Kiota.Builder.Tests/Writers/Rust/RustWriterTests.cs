using System;

using Kiota.Builder.Writers.Rust;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public class RustWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new RustWriter("./", "graph");
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new RustWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new RustWriter("./", null));
    }
}
