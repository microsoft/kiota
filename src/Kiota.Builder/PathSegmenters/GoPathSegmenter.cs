using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class GoPathSegmenter : CommonPathSegmenter
    {
        public GoPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) {}
        public override string FileSuffix => ".go";
        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            return currentElement switch
            {
                CodeNamespace => new string[] { GetLastFileNameSegment(currentElement) },// We put barrels inside namespace folders
                _ => Enumerable.Empty<string>(),
            };
        }
        public override string NormalizeFileName(CodeElement currentElement) {
            return currentElement switch
            {
                CodeNamespace => "go",
                _ => GetLastFileNameSegment(currentElement).ToSnakeCase(),
            };
        }
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToLowerInvariant();
    }
}
