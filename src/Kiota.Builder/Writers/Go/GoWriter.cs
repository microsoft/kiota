namespace Kiota.Builder.Writers.Go {
    public class GoWriter : LanguageWriter {
        public GoWriter(string rootPath, string clientNamespaceName, bool usesBackingStore)
        {
            PathSegmenter = new GoPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new GoConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService, usesBackingStore));
        }
    }
}
