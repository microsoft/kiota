using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class TypeScriptPathSegmenter : CommonPathSegmenter
    {
        public TypeScriptPathSegmenter(string rootPath, string clientNamespaceName) : base (rootPath, clientNamespaceName) { }
        public override string FileSuffix =>  ".ts";
        public override string NormalizeFileName(CodeElement currentElement) {
            return currentElement switch
            {
                CodeNamespace => "index",
                _ => GetDefaultFileName(currentElement),
            };
        }
        private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterLowerCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
        public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
        {
            return currentElement switch
            {
                CodeNamespace => new string[] { GetDefaultFileName(currentElement) },// We put barrels inside namespace folders
                _ => Enumerable.Empty<string>(),
            };
        }
    }
}
