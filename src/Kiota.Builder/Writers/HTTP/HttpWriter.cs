using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Http;

public class HttpWriter : LanguageWriter
{
    public HttpWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new HttpPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new HttpConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
    }
}
