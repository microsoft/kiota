namespace Kiota.Builder.Writers.Ruby
{
    public class RubyWriter : LanguageWriter
    {
        public RubyWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new RubyPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new RubyConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            //TODO: No Enum in Ruby
            //AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
