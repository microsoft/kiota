using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class PowerShellPathSegmenter : CommonPathSegmenter
    {
        public PowerShellPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }

        public override string FileSuffix => ".cs";

        public override string NormalizeFileName(CodeElement currentElement)
        {
            return GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
        }

        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
    }
}
