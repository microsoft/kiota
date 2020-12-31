using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace kiota.core
{
    /// <summary>
    /// Convert CodeDOM classes to strings or files
    /// </summary>
    public static class CodeRenderer
    {
        public static string RenderCodeAsString(LanguageWriter writer, CodeElement root)
        {
            var sw = new StringWriter();
            writer.SetTextWriter(sw);

            RenderCode(writer, root);
            return sw.GetStringBuilder().ToString();
        }

        public static async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeClass codeClass, string outputFile)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, codeClass);
            await sw.FlushAsync();
        }

        public static async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root)
        {
            foreach (var codeElement in root.GetChildElements())
            {
                if (codeElement is CodeClass codeClass)
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeClass, writer.PathSegmenter.GetPath(root, codeClass));
                else if(codeElement is CodeNamespace codeNamespace)
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace);
            }
        }

        private static void RenderCode(LanguageWriter writer, CodeElement element)
        {
            writer.Write(element);
            foreach (var childElement in element.GetChildElements())
            {
                RenderCode(writer, childElement);
            }
        }
    }
}
