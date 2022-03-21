using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class PowerShellPathSegmenter : CommonPathSegmenter
    {
        public PowerShellPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }

        public override string FileSuffix => ".cs";

        public override string NormalizeFileName(CodeElement currentElement)
        {
            //TODO: Flatten request builder into a PowerShell command.
            string fileName = GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
            if (currentElement is CodeMethod methodElement && methodElement.Parent.Name != ClientNamespaceName
                && methodElement.MethodKind != CodeMethodKind.ClientConstructor)
            {
                // Drop RequestBuilder and Async. These are not needed in PowerShell.
                fileName = fileName.Replace("RequestBuilder", string.Empty).Replace("Async", string.Empty);
                var parentNamespace = methodElement.Parent.Parent as CodeNamespace;
                fileName = $"{fileName}{GetNamespacePathSegments(parentNamespace, currentElement, fileName).Aggregate((x,y) => $"{x}{y}")}";
                fileName = fileName.SplitAndSingularizePascalCase().Distinct().Aggregate((x,y) => $"{x}{y}").Replace("Item", string.Empty);
            }
            return fileName;
        }

        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
    }
}
