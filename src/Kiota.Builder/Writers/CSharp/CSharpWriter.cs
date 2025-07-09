using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.CSharp;
public class CSharpWriter : LanguageWriter
{
    public CSharpWriter(string rootPath, string clientNamespaceName, bool useCSharp13)
    {
        PathSegmenter = new CSharpPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new CSharpConventionService(useCSharp13);
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));

    }
}
