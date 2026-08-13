using System;

using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers.TypeScript;

public class TypeScriptWriterTests
{
    [Fact]
    public void Instantiates()
    {
        var writer = new TypeScriptWriter("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new TypeScriptWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new TypeScriptWriter("./", null));
    }
}
