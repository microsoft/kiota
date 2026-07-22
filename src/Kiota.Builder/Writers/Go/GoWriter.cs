using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Go;

public class GoWriter : LanguageWriter
{
    // gofmt (and Go tooling in general) requires LF line endings, so Go output always uses LF
    // regardless of the host OS newline.
    protected override string LineSeparator => "\n";
    public GoWriter(string rootPath, string clientNamespaceName, bool excludeBackwardCompatible = false) : base("\t", 1)
    {
        PathSegmenter = new GoPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new GoConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeInterfaceDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService, excludeBackwardCompatible));
        AddOrReplaceCodeElementWriter(new CodeFileBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodeFileDeclarationWriter(conventionService));
    }
}
