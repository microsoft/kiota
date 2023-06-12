using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;

namespace Kiota.Builder.CodeRenderers;

/// <summary>
/// Convert CodeDOM classes to strings or files
/// </summary>
public class CodeRenderer
{
    public CodeRenderer(GenerationConfiguration configuration, CodeElementOrderComparer? elementComparer = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Configuration = configuration;
        _rendererElementComparer = elementComparer ?? (configuration.ShouldRenderMethodsOutsideOfClasses ? new CodeElementOrderComparerWithExternalMethods() : new CodeElementOrderComparer());
    }
    public async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentException.ThrowIfNullOrEmpty(outputFile);
#pragma warning disable CA2007
        await using var stream = new FileStream(outputFile, FileMode.Create);
#pragma warning restore CA2007

        var sw = new StreamWriter(stream);
        writer.SetTextWriter(sw);
        RenderCode(writer, codeElement);
        if (!cancellationToken.IsCancellationRequested)
            await sw.FlushAsync().ConfigureAwait(false); // stream writer doesn't not have a cancellation token overload https://github.com/dotnet/runtime/issues/64340
    }
    // We created barrels for code namespaces. Skipping for empty namespaces, ones created for users, and ones with same namespace as class name.
    public async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace currentNamespace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(currentNamespace);
        if (cancellationToken.IsCancellationRequested) return;
        foreach (var codeElement in currentNamespace.GetChildElements(true))
        {
            switch (codeElement)
            {
                case CodeClass:
                case CodeEnum:
                case CodeFunction:
                case CodeInterface:
                case CodeFile:
                    if (writer.PathSegmenter?.GetPath(currentNamespace, codeElement) is string path)
                        await RenderCodeNamespaceToSingleFileAsync(writer, codeElement, path, cancellationToken).ConfigureAwait(false);
                    break;
                case CodeNamespace codeNamespace:
                    await RenderBarrel(writer, currentNamespace, codeNamespace, cancellationToken).ConfigureAwait(false);
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
    private async Task RenderBarrel(LanguageWriter writer, CodeNamespace parentNamespace, CodeNamespace codeNamespace, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(codeNamespace.Name) &&
            Configuration.ShouldWriteNamespaceIndices &&
            (!Configuration.ClientNamespaceName.StartsWith(codeNamespace.Name, StringComparison.OrdinalIgnoreCase) ||
            Configuration.ClientNamespaceName.Equals(codeNamespace.Name, StringComparison.OrdinalIgnoreCase)) && // we want a barrel for the root namespace
            ShouldRenderNamespaceFile(codeNamespace) &&
            writer.PathSegmenter?.GetPath(parentNamespace, codeNamespace) is string path)
        {
            await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, path, cancellationToken).ConfigureAwait(false);
        }
    }
    private readonly CodeElementOrderComparer _rendererElementComparer;
    protected GenerationConfiguration Configuration
    {
        get; private set;
    }
    private void RenderCode(LanguageWriter writer, CodeElement element)
    {
        writer.Write(element);
        if (element is not CodeNamespace)
            foreach (var childElement in element.GetChildElements()
                                                .Order(_rendererElementComparer))
            {
                RenderCode(writer, childElement);
            }

    }

    public virtual bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        if (codeNamespace is null) return false;
        // if the module already has a class with the same name, it's going to be declared automatically
        var namespaceNameLastSegment = codeNamespace.Name.Split('.').Last();
        return Configuration.ShouldWriteBarrelsIfClassExists || codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null;
    }

    public static CodeRenderer GetCodeRender(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Language switch
        {
            GenerationLanguage.TypeScript => new TypeScriptCodeRenderer(config),
            GenerationLanguage.Python => new PythonCodeRenderer(config),
            _ => new CodeRenderer(config),
        };
    }

}
