using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Dart;
public class DartWriter : LanguageWriter
{
    public DartWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new DartPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new DartConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));

    }
}
