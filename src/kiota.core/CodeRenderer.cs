using System.IO;
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

        public static async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeNamespace root, string outputFile)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, root);
            await sw.FlushAsync();
        }

        public static async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root, string outputPath)
        {
            foreach (var codeElement in root.GetChildElements())
            {
                CodeClass codeClass = codeElement as CodeClass;
                if (codeClass is not null)
                {
                    var codeNamespace = new CodeNamespace() { Name = root.Name };
                    codeNamespace.AddUsing(root.StartBlock.Usings.ToArray());
                    codeNamespace.AddClass(codeClass);
                    await RenderCodeNamespaceToSingleFileAsync(writer, codeNamespace, Path.Combine(outputPath, codeClass.Name + writer.GetFileSuffix()));
                }
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
