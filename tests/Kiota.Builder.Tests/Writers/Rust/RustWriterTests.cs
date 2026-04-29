using System;

using Kiota.Builder.Writers.Rust;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public class RustWriterTests
{
    [Fact]
    public void WriterExists()
    {
        var writer = new RustWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
    }
    [Fact]
    public void ThrowsOnNullRootPath()
    {
        Assert.Throws<ArgumentNullException>(() => new RustWriter(null, "graph"));
    }
    [Fact]
    public void ThrowsOnNullClientNamespace()
    {
        Assert.Throws<ArgumentNullException>(() => new RustWriter("./", null));
    }
}
