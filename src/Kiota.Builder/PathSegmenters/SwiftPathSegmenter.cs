using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class SwiftPathSegmenter : CommonPathSegmenter
    {
        public SwiftPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) {}
        public override string FileSuffix => ".swift";
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
        public override string NormalizeFileName(CodeElement currentElement) {
            var fileName = GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
            var suffix = currentElement switch {
                CodeNamespace n => n.Name.GetNamespaceImportSymbol(string.Empty),
                CodeClass c => c.GetImmediateParentOfType<CodeNamespace>().Name.GetNamespaceImportSymbol(string.Empty),
                _ => string.Empty,
            };
            return fileName + suffix;
        }
    }
}
