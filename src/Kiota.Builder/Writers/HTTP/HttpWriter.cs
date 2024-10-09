using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.http;

public class HttpWriter : LanguageWriter
{
    public HttpWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new HttpPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new HttpConventionService(clientNamespaceName);
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeNamespaceWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
    }
}
