namespace Kiota.Builder.Writers.Ruby
{
    public class RubyWriter : LanguageWriter
    {
        public RubyWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new RubyPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new RubyConventionService();
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService, clientNamespaceName));
            AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeNamespaceWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        }
    }
}
