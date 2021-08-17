using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class GoPathSegmenter : CommonPathSegmenter
    {
        public GoPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) {}
        public override string GetFileSuffix(CodeElement currentElement) => ".go";
        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            switch(currentElement) {
                case CodeNamespace ns:
                    return new string[] { GetLastFileNameSegment(currentElement) }; // We put barrels inside namespace folders
                default:
                    return Enumerable.Empty<string>();
            }
        }
        public override string NormalizeFileName(CodeElement currentElement) {
            switch (currentElement) {
                case CodeNamespace ns:
                    return "go";
                default:
                    return GetLastFileNameSegment(currentElement).ToSnakeCase();
            }
        }
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToLowerInvariant();
    }
}
