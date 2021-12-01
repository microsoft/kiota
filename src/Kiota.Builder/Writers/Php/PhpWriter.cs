namespace Kiota.Builder.Writers.Php
{
    public class PhpWriter: LanguageWriter
    {
        public PhpWriter(string rootPath, string clientNamespaceName, bool useBackingStore = false)
        {
            PathSegmenter = new PhpPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new PhpConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
        }
    }
}
