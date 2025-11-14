using System;

using Kiota.Builder.Writers.CSharp;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public class CSharpWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new CSharpWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new CSharpWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new CSharpWriter("./", null));
    }
}
