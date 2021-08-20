using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class PhpPathSegmenter : CommonPathSegmenter
    {
        public PhpPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".php";

        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            switch(currentElement) {
                case CodeNamespace:
                    return new[] { GetLastFileNameSegment(currentElement) }; // We put barrels inside namespace folders
                default:
                    return Enumerable.Empty<string>();
            }
        }

        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
        public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
    }
}
