using System;
using System.Dynamic;
using System.IO;
using System.Linq;
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
        public async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, codeElement);
            await sw.FlushAsync();
        }
        // We created barrells for codenamespaces. Skipping for empty namespaces, ones created for users, and ones with same namspace as class name.
        public async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root)
        {
            foreach (var codeElement in root.GetChildElements(true))
            {
                if (codeElement is CodeClass codeClass)
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeClass, writer.PathSegmenter.GetPath(root, codeClass));
                else if (codeElement is CodeEnum codeEnum)
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeEnum, writer.PathSegmenter.GetPath(root, codeEnum));
                else if (codeElement is CodeNamespace codeNamespace)
                {
                    if (!string.IsNullOrEmpty(codeNamespace.Name) && !string.IsNullOrEmpty(root.Name) &&
                        _configuration.ShouldWriteNamespaceIndices &&
                        !_configuration.ClientNamespaceName.Contains(codeNamespace.Name, StringComparison.OrdinalIgnoreCase))
                    {
                       
                        if(ShouldRenderNamespaceFile(codeNamespace))                    
                        {
                            await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, writer.PathSegmenter.GetPath(root, codeNamespace));
                        }
                     
                    }
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace);
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
