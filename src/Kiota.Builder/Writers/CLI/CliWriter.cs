using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Cli;
class CliWriter : CSharpWriter
{
    public CliWriter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName, false)
    {
        var conventionService = new CSharpConventionService(false);
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CliCodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
    }
}
