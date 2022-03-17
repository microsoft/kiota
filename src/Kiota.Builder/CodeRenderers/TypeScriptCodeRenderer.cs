using System.Linq;

namespace Kiota.Builder.CodeRenderers
{
    public class TypeScriptCodeRenderer : CodeRenderer
    {
        public TypeScriptCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
        public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
        {
            var classes = codeNamespace.Classes;
            return classes.Any(c => c.IsOfKind(CodeClassKind.Model));
        }
    }
}
