namespace Kiota.Builder.Writers.Markdown
{
    public class MarkdownWriter : LanguageWriter
    {
        public MarkdownWriter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new MarkdownPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new MarkdownConventionService();
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));

        }
    }
}
