using System.Linq;

namespace Kiota.Builder.CodeRenderers
{
    public class TypeScriptCodeRenderer: CodeRenderer
    {
        public TypeScriptCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
        public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
        {
            return (codeNamespace.Classes.Any() || codeNamespace.Enums.Any());
        }
    }
}
