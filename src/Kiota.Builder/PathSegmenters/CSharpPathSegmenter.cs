using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class CSharpPathSegmenter : CommonPathSegmenter
    {
        public CSharpPathSegmenter(string rootPath, string clientNamespaceName): base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".cs";
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
        public override string NormalizeFileName(string elementName) => elementName.ToFirstCharacterUpperCase();
    }
}
