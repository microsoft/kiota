using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder {
    public class PythonPathSegmenter : CommonPathSegmenter
    {
        public PythonPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
        public override string FileSuffix => ".py";
        public override string NormalizeFileName(CodeElement currentElement) {
            return currentElement switch
            {
                CodeNamespace => "__init__",
                _ => GetDefaultFileName(currentElement)
            };
        }
        private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();
        public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToSnakeCase();
    }
}

