using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.CodeRenderers;
public class TypeScriptCodeRenderer : CodeRenderer
{
    public TypeScriptCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
    public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        if (codeNamespace is null) return false;
        return codeNamespace.Interfaces.Any();
    }
}
