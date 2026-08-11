using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Php;

public class PhpWriter : LanguageWriter
{
    public PhpWriter(string rootPath, string clientNamespaceName, bool useBackingStore = false)
    {
        PathSegmenter = new PhpPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new PhpConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService, useBackingStore));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
    }
}
