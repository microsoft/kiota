using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kiota.Builder {
    public abstract class CommonPathSegmenter : IPathSegmenter {
        protected CommonPathSegmenter(string rootPath, string clientNamespaceName)
        {
            ClientNamespaceName = clientNamespaceName ?? throw new ArgumentNullException(nameof(clientNamespaceName));
            RootPath = (rootPath?.Contains(Path.DirectorySeparatorChar) ?? true ? rootPath : rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) ?? throw new ArgumentNullException(nameof(rootPath));
        }
        internal readonly string ClientNamespaceName;
        private readonly string RootPath;
        public abstract string FileSuffix { get; }
        public abstract string NormalizeNamespaceSegment(string segmentName);
        public abstract string NormalizeFileName(CodeElement currentElement);
        public virtual IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName) => Enumerable.Empty<string>();
        protected static string GetLastFileNameSegment(CodeElement currentElement) => currentElement.Name.Split('.').Last();
        public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement) {
            var fileName = NormalizeFileName(currentElement);
            var namespacePathSegments = GetNamespacePathSegments(currentNamespace, currentElement, fileName);
            var targetPath = Path.Combine(RootPath, namespacePathSegments.Any() ? namespacePathSegments                                           
                                            .Aggregate((x, y) => $"{x}{Path.DirectorySeparatorChar}{y}") : string.Empty,
                                            fileName + FileSuffix);
            var directoryPath = Path.GetDirectoryName(targetPath);
            Directory.CreateDirectory(directoryPath);
            return targetPath;
        }
        public IList<string> GetNamespacePathSegments(CodeNamespace currentNamespace, CodeElement currentElement, string fileName)
        {
            var namespacePathSegments = new List<string>(currentNamespace.Name
                                            .Replace(ClientNamespaceName, string.Empty)
                                            .TrimStart('.')
                                            .Split('.'));
            namespacePathSegments.AddRange(GetAdditionalSegment(currentElement, fileName)); //Union removes duplicates so we're building a list instead to conserve those.
            namespacePathSegments = namespacePathSegments.Where(x => !string.IsNullOrEmpty(x))
                                            .Select(x => NormalizeNamespaceSegment(x))
                                            .ToList();
            return namespacePathSegments;
        }
    }
}
