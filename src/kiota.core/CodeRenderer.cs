using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
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

        public static void RenderCodeNamespaceToSingleFile(LanguageWriter writer, CodeNamespace root, string outputFile)
        {
            using var stream = new FileStream(outputFile, FileMode.Create);

            var sw = new StreamWriter(stream);
            writer.SetTextWriter(sw);
            RenderCode(writer, root);
            sw.Flush();
        }

        public static void RenderCodeNamespaceToFilePerClass(LanguageWriter writer, CodeNamespace root, string outputPath)
        {
            foreach (var codeElement in root.GetChildElements())
            {
                CodeClass codeClass = codeElement as CodeClass;
                if (codeClass is not null)
                {
                    var codeNamespace = new CodeNamespace() { Name = root.Name };
                    codeNamespace.AddUsing(((CodeNamespace.Declaration)root.StartBlock).Usings);
                    codeNamespace.AddClass(codeClass);
                    RenderCodeNamespaceToSingleFile(writer, codeNamespace, Path.Combine(outputPath, codeClass.Name + writer.GetFileSuffix()));
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
