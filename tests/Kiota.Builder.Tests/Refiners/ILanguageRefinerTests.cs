using System;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kiota.Builder.Tests.Refiners;

public class ILanguageRefinerTests
{
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
}
