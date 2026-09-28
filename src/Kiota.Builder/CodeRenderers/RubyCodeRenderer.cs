using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers.Ruby;

namespace Kiota.Builder.CodeRenderers;

public class RubyCodeRenderer : CodeRenderer
{
    public RubyCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }
    public override bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace) =>
        codeNamespace is not null && base.ShouldRenderNamespaceFile(codeNamespace) && RubyConventionService.HasAutoloadedMembers(codeNamespace);
}
