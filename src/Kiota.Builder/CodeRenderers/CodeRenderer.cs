using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;

namespace Kiota.Builder.CodeRenderers
{
    /// <summary>
    /// Convert CodeDOM classes to strings or files
    /// </summary>
    public class CodeRenderer
    {
        public CodeRenderer(GenerationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _configuration = configuration;
            _rendererElementComparer = configuration.ShouldRenderMethodsOutsideOfClasses ? new CodeElementOrderComparerWithExternalMethods() : new CodeElementOrderComparer();
        }
        public async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, codeElement);
            if(!cancellationToken.IsCancellationRequested)
                await sw.FlushAsync(); // stream writer doesn't not have a cancellation token overload https://github.com/dotnet/runtime/issues/64340
        }
        // We created barrels for code namespaces. Skipping for empty namespaces, ones created for users, and ones with same namespace as class name.
        public async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace currentNamespace, CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested) return;
            foreach (var codeElement in currentNamespace.GetChildElements(true))
            {
                switch(codeElement) {
                    case CodeClass:
                    case CodeEnum:
                    case CodeFunction:
                    case CodeInterface:
                        if (writer.PathSegmenter?.GetPath(currentNamespace, codeElement) is string path)
                            await RenderCodeNamespaceToSingleFileAsync(writer, codeElement, path, cancellationToken);
                        break;
                    case CodeNamespace codeNamespace:
                        await RenderBarrel(writer, currentNamespace, codeNamespace, cancellationToken);
                        await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken);
                    break;
                }
            }
        }
        private async Task RenderBarrel(LanguageWriter writer, CodeNamespace parentNamespace, CodeNamespace codeNamespace, CancellationToken cancellationToken) {
            if (!string.IsNullOrEmpty(codeNamespace.Name) &&
                _configuration.ShouldWriteNamespaceIndices &&
                (!_configuration.ClientNamespaceName.StartsWith(codeNamespace.Name, StringComparison.OrdinalIgnoreCase) || 
                _configuration.ClientNamespaceName.Equals(codeNamespace.Name, StringComparison.OrdinalIgnoreCase)) && // we want a barrel for the root namespace
                ShouldRenderNamespaceFile(codeNamespace) && 
                writer.PathSegmenter?.GetPath(parentNamespace, codeNamespace) is string path)
            {
                await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, path, cancellationToken);
            }
        }
        private readonly CodeElementOrderComparer _rendererElementComparer;
        protected readonly GenerationConfiguration _configuration;
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
            // if the module already has a class with the same name, it's going to be declared automatically
            var namespaceNameLastSegment = codeNamespace.Name.Split('.').Last().ToLowerInvariant();
            return _configuration.ShouldWriteBarrelsIfClassExists || codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null;
        }

        public static CodeRenderer GetCodeRender(GenerationConfiguration config) => 
            config.Language switch
            {
                GenerationLanguage.TypeScript => new TypeScriptCodeRenderer(config),
                _ => new CodeRenderer(config),
            };
    
    }
}
