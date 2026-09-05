using System;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Python;

public class PythonWriter : LanguageWriter
{
    public PythonWriter(string rootPath, string clientNamespaceName, bool usesBackingStore = false)
    {
        ArgumentNullException.ThrowIfNull(clientNamespaceName);
        var normalizedClientNamespaceName = string.Join('.',
            clientNamespaceName
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(static x => x.ToSnakeCase()));
        PathSegmenter = new PythonPathSegmenter(rootPath, normalizedClientNamespaceName);
        var conventionService = new PythonConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService, normalizedClientNamespaceName));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter());
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService, normalizedClientNamespaceName, usesBackingStore));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService, normalizedClientNamespaceName));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeNameSpaceWriter(conventionService));
    }
}
