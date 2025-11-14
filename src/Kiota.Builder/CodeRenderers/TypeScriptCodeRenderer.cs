using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.CodeRenderers;

public class TypeScriptCodeRenderer : CodeRenderer
{
    private CodeNamespace? modelsNamespace;
    public TypeScriptCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
    public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        if (codeNamespace is null) return false;
        modelsNamespace ??= codeNamespace.GetRootNamespace().FindChildByName<CodeNamespace>(Configuration.ModelsNamespaceName);
        if (modelsNamespace is not null && !modelsNamespace.IsParentOf(codeNamespace) && modelsNamespace != codeNamespace) return false;
        return codeNamespace.Interfaces.Any() || codeNamespace.Files.Any(static x => x.Interfaces.Any());
    }
}
