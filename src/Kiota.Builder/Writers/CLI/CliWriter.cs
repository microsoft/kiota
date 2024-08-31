using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Cli;
class CliWriter : CSharpWriter
{
    public CliWriter(string rootPath, string clientNamespaceName, string? clientClassAccessModifier) : base(rootPath, clientNamespaceName, clientClassAccessModifier)
    {
        var conventionService = new CSharpConventionService();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService, clientClassAccessModifier));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CliCodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
    }
}
