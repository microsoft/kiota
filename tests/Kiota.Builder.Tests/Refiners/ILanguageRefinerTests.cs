using System;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class ILanguageRefinerTests
{
    [Theory]
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.PHP)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.HTTP)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.Dart)]
    public async Task AcceptsLoggerForEveryLanguage(GenerationLanguage language)
    {
        foreach (var logger in new ILogger[] { NullLogger<ILanguageRefinerTests>.Instance, null })
        {
            var configuration = new GenerationConfiguration { Language = language };
            CommonLanguageRefiner refiner = language switch
            {
                GenerationLanguage.CSharp => new CSharpRefiner(configuration, logger),
                GenerationLanguage.TypeScript => new TypeScriptRefiner(configuration, logger),
                GenerationLanguage.Java => new JavaRefiner(configuration, logger),
                GenerationLanguage.Ruby => new RubyRefiner(configuration, logger),
                GenerationLanguage.PHP => new PhpRefiner(configuration, logger),
                GenerationLanguage.Go => new GoRefiner(configuration, logger),
                GenerationLanguage.HTTP => new HttpRefiner(configuration, logger),
                GenerationLanguage.Python => new PythonRefiner(configuration, logger),
                GenerationLanguage.Dart => new DartRefiner(configuration, logger),
                _ => throw new ArgumentOutOfRangeException(nameof(language)),
            };

            await refiner.RefineAsync(CodeNamespace.InitRootNamespace(), TestContext.Current.CancellationToken);
            await ILanguageRefiner.RefineAsync(configuration, CodeNamespace.InitRootNamespace(), logger, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public void RetainsLoggerAndDefaultsToNullLogger()
    {
        var configuration = new GenerationConfiguration();
        var logger = NullLogger<ILanguageRefinerTests>.Instance;

        Assert.Same(logger, new LoggerTestRefiner(configuration, logger).CurrentLogger);
        Assert.Same(NullLogger.Instance, new LoggerTestRefiner(configuration).CurrentLogger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsNullGeneratedCodeForBothOverloads(bool withLogger)
    {
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.Dart };

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => withLogger
            ? ILanguageRefiner.RefineAsync(configuration, null, NullLogger.Instance, TestContext.Current.CancellationToken)
            : ILanguageRefiner.RefineAsync(configuration, null, TestContext.Current.CancellationToken));

        Assert.Equal("generatedCode", exception.ParamName);
    }

    private sealed class LoggerTestRefiner(GenerationConfiguration configuration, ILogger suppliedLogger = null) : CommonLanguageRefiner(configuration, suppliedLogger)
    {
        public ILogger CurrentLogger => Logger;
        public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
