using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Go;

public class GoWriter : LanguageWriter
{
    public GoWriter(string rootPath, string clientNamespaceName, bool excludeBackwardCompatible = false)
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
