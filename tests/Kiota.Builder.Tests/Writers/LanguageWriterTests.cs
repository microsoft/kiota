using Kiota.Builder.Writers;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Python;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.TypeScript;

using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class LanguageWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    [Fact]
    public void GetCorrectWriterForLanguage()
    {
        Assert.Equal(typeof(CSharpWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(JavaWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(RubyWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(TypeScriptWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(GoWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(PhpWriter), LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName).GetType());
        Assert.Equal(typeof(PythonWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.Python, DefaultPath, DefaultName).GetType());
    }
}
