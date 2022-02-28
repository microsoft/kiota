using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.Writers;

namespace Kiota.Builder
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
                        var namespaceNameLastSegment = codeNamespace.Name.Split('.').Last().ToLowerInvariant();
                        // if the module already has a class with the same name, it's going to be declared automatically
                        if (_configuration.ShouldWriteBarrelsIfClassExists && _configuration.setCodeRenderingCondition(codeNamespace))
                         // TODO : Verify and plug the following condition in the language specific index rendering condition 
                        //codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null)
                         
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
    }
}
