using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class PhpPathSegmenter : CommonPathSegmenter
    {
        public PhpPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".php";
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
        public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
    }
}
