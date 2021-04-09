using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class JavaPathSegmenter : CommonPathSegmenter
    {
        public JavaPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".java";
        public override string NormalizeFileName(string elementName) => elementName.ToFirstCharacterUpperCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
    }
}
