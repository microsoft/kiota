using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class GoPathSegmenter : CommonPathSegmenter
    {
        public GoPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) {}
        public override string FileSuffix => ".go";
        public override string NormalizeFileName(string elementName) => elementName.ToSnakeCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
    }
}
