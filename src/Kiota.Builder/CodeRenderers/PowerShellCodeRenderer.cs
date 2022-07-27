using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Writers;

namespace Kiota.Builder.CodeRenderers
{
    public class PowerShellCodeRenderer : CodeRenderer
    {
        public PowerShellCodeRenderer(GenerationConfiguration configuration) : base(configuration) { }

        public override async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace root, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            foreach (var codeElement in root.GetChildElements(true))
            {
                switch (codeElement)
                {
                    case CodeEnum:
                    case CodeFunction:
                    case CodeInterface:
                        await RenderCodeNamespaceToSingleFileAsync(writer, codeElement, writer.PathSegmenter.GetPath(root, codeElement), cancellationToken);
                        break;
                    case CodeClass codeClass:
                        await RenderClassAsync(writer, root, codeClass, cancellationToken);
                        break;
                    case CodeNamespace codeNamespace:
                        await RenderBarrel(writer, root, codeNamespace, cancellationToken);
                        await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken);
                        break;
                }
            }
        }

        private async Task RenderClassAsync(LanguageWriter writer, CodeNamespace root, CodeClass codeClass, CancellationToken cancellationToken)
        {
            var filePath = codeClass.IsOfKind(CodeClassKind.RequestBuilder) ? writer.PathSegmenter.GetPath(codeClass, "Cmdlets") : writer.PathSegmenter.GetPath(root, codeClass);
            await RenderCodeNamespaceToSingleFileAsync(writer, codeClass, filePath, cancellationToken);
        }
    }
}
