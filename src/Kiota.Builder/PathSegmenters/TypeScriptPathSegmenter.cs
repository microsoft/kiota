using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class TypeScriptPathSegmenter : CommonPathSegmenter
    {
        public TypeScriptPathSegmenter(string rootPath, string clientNamespaceName) : base (rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".ts";
        public override string NormalizeFileName(string elementName) => elementName.ToFirstCharacterLowerCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
    }
}
