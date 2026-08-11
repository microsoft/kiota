using System;
using System.IO;

using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php;

public sealed class PhpWriterTests : IDisposable
{
    private readonly StringWriter tw;
    private const string OutputPath = "./";
    private const string NameSpace = "Namespace";
    private readonly LanguageWriter writer;
    public PhpWriterTests()
    {
        tw = new StringWriter();
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, OutputPath, NameSpace);

        writer.SetTextWriter(tw);
    }

    [Fact]
    public void WriteSomething()
    {
        var result = tw.ToString();
        Assert.Empty(result);
        Assert.NotNull(writer.PathSegmenter);
        Assert.IsType<PhpWriter>(writer);
        Assert.Throws<ArgumentNullException>(() => new PhpWriter(null, "graph"));
        Assert.Throws<ArgumentNullException>(() => new PhpWriter("./", null));
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
}
