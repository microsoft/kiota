using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _rendererElementComparer = configuration.ShouldRenderMethodsOutsideOfClasses ? new CodeElementOrderComparerWithExternalMethods() : new CodeElementOrderComparer();
        }
        public async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, codeElement);
            if(!cancellationToken.IsCancellationRequested)
                await sw.FlushAsync(); // streamwriter doesn't not have a cancellation token overload https://github.com/dotnet/runtime/issues/64340
        }
        // We created barrells for codenamespaces. Skipping for empty namespaces, ones created for users, and ones with same namspace as class name.
        public async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root, CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested) return;
            foreach (var codeElement in root.GetChildElements(true))
            {
                switch(codeElement) {
                    case CodeClass:
                    case CodeEnum:
                    case CodeFunction:
                    case CodeInterface:
                        await RenderCodeNamespaceToSingleFileAsync(writer, codeElement, writer.PathSegmenter.GetPath(root, codeElement), cancellationToken);
                        break;
                    case CodeNamespace codeNamespace:
                        await RenderBarrel(writer, root, codeNamespace, cancellationToken);
                        await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken);
                    break;
                }
            }
        }
        private async Task RenderBarrel(LanguageWriter writer, CodeNamespace root, CodeNamespace codeNamespace, CancellationToken cancellationToken) {
            if (!string.IsNullOrEmpty(codeNamespace.Name) &&
                !string.IsNullOrEmpty(root.Name) &&
                _configuration.ShouldWriteNamespaceIndices &&
                !_configuration.ClientNamespaceName.Contains(codeNamespace.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (ShouldRenderNamespaceFile(codeNamespace))
                {
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, writer.PathSegmenter.GetPath(root, codeNamespace), cancellationToken);
                }
            }
        }
        private readonly CodeElementOrderComparer _rendererElementComparer;
        private readonly GenerationConfiguration _configuration;
        private void RenderCode(LanguageWriter writer, CodeElement element)
        {
            writer.Write(element);
            if (!(element is CodeNamespace))
                foreach (var childElement in element.GetChildElements()
                                                   .OrderBy(x => x, _rendererElementComparer))
                {
                    RenderCode(writer, childElement);
                }

        }

        public virtual  bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
        {
            // if the module already has a class with the same name, it's going to be declared automatically
            var namespaceNameLastSegment = codeNamespace.Name.Split('.').Last().ToLowerInvariant();
            return (_configuration.ShouldWriteBarrelsIfClassExists || codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null);
        }

        public static CodeRenderer GetCodeRender(GenerationConfiguration config)
        {
            switch (config.Language)
            {
                case GenerationLanguage.TypeScript:
                    return new TypeScriptCodeRenderer(config);
                default:
                    return new CodeRenderer(config);
               
            }
        }
    
    }
}
