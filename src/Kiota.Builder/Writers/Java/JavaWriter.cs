namespace Kiota.Builder.Writers.Java
{
    public class JavaWriter : LanguageWriter
    {
        public JavaWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new JavaPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new JavaConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
