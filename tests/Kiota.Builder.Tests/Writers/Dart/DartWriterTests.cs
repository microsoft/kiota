using System;

using Kiota.Builder.Writers.Dart;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public class DartWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new DartWriter("./", "graph");
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new DartWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new DartWriter("./", null));
    }
}
