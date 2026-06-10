using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Rust;

public class RustWriter : LanguageWriter
{
    public RustWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new RustPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new RustConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeFileBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodeFileDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeNamespaceWriter(conventionService));
    }
}
