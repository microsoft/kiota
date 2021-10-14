namespace Kiota.Builder.Writers.Swift {
    public class SwiftWriter : LanguageWriter {
        public SwiftWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new SwiftPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new SwiftConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
        }
    }
}
