namespace Kiota.Builder.Writers.Swift {
    public class SwiftWriter : LanguageWriter {
        public SwiftWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new SwiftPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new SwiftConventionService();
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeNamespaceWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        }
    }
}
