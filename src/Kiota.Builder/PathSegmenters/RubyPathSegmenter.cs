using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class RubyPathSegmenter : CommonPathSegmenter
    {
        public RubyPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".rb";
        public override string NormalizeFileName(string elementName) => elementName.ToSnakeCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToSnakeCase();
    }
}
