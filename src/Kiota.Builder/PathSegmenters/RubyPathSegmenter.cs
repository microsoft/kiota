using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class RubyPathSegmenter : CommonPathSegmenter
    {
        public RubyPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            switch(currentElement) {
                case CodeNamespace ns:
                    return new string[] { fileName }; // We put barrels inside namespace folders
                default:
                    return Enumerable.Empty<string>();
            }
        }
        public override string FileSuffix => ".rb";
        public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToSnakeCase();
    }
}
