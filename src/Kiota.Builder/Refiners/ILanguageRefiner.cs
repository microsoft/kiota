using System;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Refiners;

public interface ILanguageRefiner
{
    Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken);
    public static Task RefineAsync(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken cancellationToken = default) =>
        RefineAsync(config, generatedCode, null, cancellationToken);
    public static async Task RefineAsync(GenerationConfiguration config, CodeNamespace generatedCode, ILogger? logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(generatedCode);
        switch (config.Language)
        {
            case GenerationLanguage.CSharp:
                await new CSharpRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.TypeScript:
                await new TypeScriptRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Java:
                await new JavaRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Ruby:
                await new RubyRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.PHP:
                await new PhpRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Go:
                await new GoRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.HTTP:
                await new HttpRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Python:
                await new PythonRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Dart:
                await new DartRefiner(config, logger).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
