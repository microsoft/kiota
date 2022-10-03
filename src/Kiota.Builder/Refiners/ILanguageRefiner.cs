using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.Refiners;
public interface ILanguageRefiner
{
    Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken);
    public static async Task Refine(GenerationConfiguration config, CodeNamespace generatedCode, CancellationToken cancellationToken = default) {
        switch (config.Language)
        {
            case GenerationLanguage.CSharp:
                await new CSharpRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.TypeScript:
                await new TypeScriptRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Java:
                await new JavaRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Ruby:
                await new RubyRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.PHP:
                await new PhpRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Go:
                await new GoRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Shell:
                await new ShellRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Swift:
                await new SwiftRefiner(config).Refine(generatedCode, cancellationToken);
                break;
            case GenerationLanguage.Python:
                await new PythonRefiner(config).Refine(generatedCode, cancellationToken);
                break;
        }
    }
}
