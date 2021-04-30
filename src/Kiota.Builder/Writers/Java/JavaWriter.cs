namespace Kiota.Builder.Writers.Java
{
    public class JavaWriter : LanguageWriter
    {
        public JavaWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new JavaPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new JavaConventionService();
            Writers = new() {
                { typeof(CodeClass.Declaration), new CodeClassDeclarationWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeClass.End), new CodeClassEndWriter() as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeEnum), new CodeEnumWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeMethod), new CodeMethodWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeProperty), new CodePropertyWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
                { typeof(CodeType), new CodeTypeWriter(conventionService) as object as ICodeElementWriter<CodeElement> },
            };
        }
    }
}
