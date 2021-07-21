namespace Kiota.Builder.Writers.Ruby
{
    public class RubyWriter : LanguageWriter
    {
        public RubyWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new RubyPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new RubyConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter(conventionService));
            AddCodeElementWriter(new CodeNamespaceWriter(conventionService));
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
        }
    }
}
