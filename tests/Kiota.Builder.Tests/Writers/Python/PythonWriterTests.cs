using System;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
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

    [Fact]
    public void NormalizesClientNamespacePrefix()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var writer = new PythonWriter(outputPath, "graphSdk");
            var root = CodeNamespace.InitRootNamespace();
            var childNamespace = root.AddNamespace("graph_sdk.some_namespace");
            var model = childNamespace.AddClass(new CodeClass { Name = "TestModel" });

            var path = writer.PathSegmenter!.GetPath(childNamespace, model.First(), false);

            Assert.EndsWith(
                Path.Combine("some_namespace", "test_model.py"),
                path,
                StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
        }
    }
}
