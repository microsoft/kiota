namespace Kiota.Builder.Writers.Java
{
    public class JavaWriter : LanguageWriter
    {
        public JavaWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new JavaPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new JavaConventionService();
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeClassEndWriter());
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
