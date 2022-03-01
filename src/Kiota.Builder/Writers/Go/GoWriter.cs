namespace Kiota.Builder.Writers.Go {
    public class GoWriter : LanguageWriter {
        public GoWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new GoPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new GoConventionService();
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeClassEndWriter());
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        }
    }
}
