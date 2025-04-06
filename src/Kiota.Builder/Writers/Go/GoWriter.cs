using System;
using System.Linq;
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

    // Override the Ident functions as golang indents with tabs instead of spaces
    private int currentIndent;
    private static readonly string indentString = Enumerable.Repeat("\t", 1000).Aggregate(static (x, y) => x + y);
    public override void IncreaseIndent(int factor = 1)
    {
        currentIndent += 1;
    }

    public override void DecreaseIndent()
    {
        currentIndent -= 1;
    }

    public override string GetIndent()
    {
        return indentString[..Math.Max(0, currentIndent)];
    }
}
