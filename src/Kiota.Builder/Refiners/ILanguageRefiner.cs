using System;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Refiners;
public interface ILanguageRefiner
{
    Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken);
    public static async Task RefineAsync(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
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
            case GenerationLanguage.CLI:
                await new CliRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Swift:
                await new SwiftRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.HTTP:
                await new HttpRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Python:
                await new PythonRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.Dart:
                await new DartRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
            case GenerationLanguage.AL:
                await new ALRefiner(config).RefineAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                break;
        }
    }
}
