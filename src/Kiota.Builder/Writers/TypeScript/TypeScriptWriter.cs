namespace Kiota.Builder.Writers.TypeScript
{
    public class TypeScriptWriter : LanguageWriter
    {
        public TypeScriptWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new TypeScriptPathSegmenter(rootPath,clientNamespaceName);
            var conventionService = new TypeScriptConventionService(null);
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
