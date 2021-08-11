using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class TypeScriptPathSegmenter : CommonPathSegmenter
    {
        public TypeScriptPathSegmenter(string rootPath, string clientNamespaceName) : base (rootPath, clientNamespaceName) { }
        public override string GetFileSuffix(CodeElement currentElement) =>  ".ts";
        public override string NormalizeFileName(CodeElement currentElement) {
            switch(currentElement) {
                case CodeNamespace ns:
                    return "index";
                default:
                    return GetDefaultFileName(currentElement);
            }
        }
        private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterLowerCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            switch(currentElement) {
                case CodeNamespace ns:
                    return new string[] { GetDefaultFileName(currentElement) }; // We put barrels inside namespace folders
                default:
                    return Enumerable.Empty<string>();
            }
        }
    }
}
