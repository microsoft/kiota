using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder {
    public class MarkdownPathSegmenter : CommonPathSegmenter
    {
        public MarkdownPathSegmenter(string rootPath, string clientNamespaceName): base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".md";
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
        public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
    }
}
