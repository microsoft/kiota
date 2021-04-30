using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers
{
    public class CSharpWriter : LanguageWriter
    {
        public CSharpWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new CSharpPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new CSharpConventionService();
            Writers = new() {
                { typeof(CodeClass.Declaration), new CodeClassDeclarationWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeClass.End), new CodeClassEndWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeEnum), new CodeEnumWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeIndexer), new CodeIndexerWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeMethod), new CodeMethodWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeProperty), new CodePropertyWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeType), new CodeTypeWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
            };
        }
    }
}
