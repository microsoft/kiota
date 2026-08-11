using System;

using Kiota.Builder.Writers.Python;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Python;

public class PythonWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new PythonWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new PythonWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new PythonWriter("./", null));
    }
}
