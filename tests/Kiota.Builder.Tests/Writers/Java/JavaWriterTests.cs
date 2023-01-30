using System;

using Kiota.Builder.Writers.Java;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Java;
public class JavaWriterTests {
    [Fact]
    public void Instanciates() {
        var writer =  new JavaWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new JavaWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new JavaWriter("./", null));
    }
}
