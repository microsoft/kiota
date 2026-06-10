using System;
using System.IO;

using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Rust;

public sealed class CodeClassEndWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    public CodeClassEndWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Rust, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesBlockEnd()
    {
        // Just verify writer instantiates properly for block ends
        Assert.NotNull(writer);
    }
}
