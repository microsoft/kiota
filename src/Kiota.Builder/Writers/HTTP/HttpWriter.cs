using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Http;

public class HttpWriter : LanguageWriter
{
    public HttpWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new HttpPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new HttpConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new GenericCodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new GenericCodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new GenericCodeElementWriter(conventionService));
    }
}
