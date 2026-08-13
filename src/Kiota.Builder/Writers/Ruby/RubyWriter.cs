using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Ruby;

public class RubyWriter : LanguageWriter
{
    public RubyWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new RubyPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new RubyConventionService();
        var pathSegmenter = new RubyPathSegmenter(rootPath, clientNamespaceName);
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService, clientNamespaceName, pathSegmenter));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeNamespaceWriter(conventionService, pathSegmenter));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
    }
}
