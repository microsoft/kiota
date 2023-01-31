using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Python;
public class PythonWriter : LanguageWriter
{
    public PythonWriter(string rootPath, string clientNamespaceName, bool usesBackingStore = false)
    {
        PathSegmenter = new PythonPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new PythonConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService, usesBackingStore));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeNameSpaceWriter(conventionService));
    }
}
