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
    public static class CodeRenderer
    {
        public static async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, codeElement);
            await sw.FlushAsync();
        }
        // We created barrells for codenamespaces. Skipping for empty namespaces, ones created for users, and ones with same namspace as class name.
        public static async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root, bool shouldWriteNamespaceIndices, string namespacePrefix)
        {
            foreach (var codeElement in root.GetChildElements(true))
            {
                if (codeElement is CodeClass codeClass)
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeClass, writer.PathSegmenter.GetPath(root, codeClass));
                else if (codeElement is CodeEnum codeEnum)
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeEnum, writer.PathSegmenter.GetPath(root, codeEnum));
                else if(codeElement is CodeNamespace codeNamespace) {
                    
                    if(!string.IsNullOrEmpty(codeNamespace.Name) && !string.IsNullOrEmpty(root.Name) && shouldWriteNamespaceIndices && !namespacePrefix.Contains(codeNamespace.Name, StringComparison.OrdinalIgnoreCase)) {
                        var namespaceNameLastSegment = codeNamespace.Name.Split('.').Last().ToLowerInvariant();
                        // for ruby if the module already has a class with the same name, it's going to be declared automatically
                        if(codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null)
                            await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, writer.PathSegmenter.GetPath(root, codeNamespace));
                    }
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, shouldWriteNamespaceIndices, namespacePrefix);
                }
            }
        }
        private static readonly CodeElementOrderComparer rendererElementComparer = new CodeElementOrderComparer();
        private static void RenderCode(LanguageWriter writer, CodeElement element)
        {
            writer.Write(element);
            if(!(element is CodeNamespace))
                foreach (var childElement in element.GetChildElements()
                                                   .OrderBy(x => x, rendererElementComparer))
                {
                    RenderCode(writer, childElement);
                }

        }
    }
}
