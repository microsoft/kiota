using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

namespace Kiota.Builder.CodeRenderers;
public class TypeScriptCodeRenderer : CodeRenderer
{
    public TypeScriptCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
    public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        return codeNamespace.Classes.Any(static c => c.IsOfKind(CodeClassKind.Model));
    }
}
