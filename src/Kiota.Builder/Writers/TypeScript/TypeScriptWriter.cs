using Kiota.Builder.PathSegmenters;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Writers.TypeScript;

public class TypeScriptWriter : LanguageWriter
{
    public TypeScriptWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new TypeScriptPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = ConventionServiceInstance;
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService, clientNamespaceName));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeFunctionWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeInterfaceDeclarationWriter(conventionService, clientNamespaceName));
        AddOrReplaceCodeElementWriter(new CodeFileBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeFileDeclarationWriter(conventionService, clientNamespaceName));
        AddOrReplaceCodeElementWriter(new CodeConstantWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeUnionTypeWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIntersectionTypeWriter(conventionService));
    }
}
