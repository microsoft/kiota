using System;
using System.IO;
using System.Linq;

namespace Kiota.Builder {
    public abstract class CommonPathSegmenter : IPathSegmenter {
        protected CommonPathSegmenter(string rootPath, string clientNamespaceName)
        {
            ClientNamespaceName = clientNamespaceName ?? throw new ArgumentNullException(nameof(clientNamespaceName));
            RootPath = (rootPath?.Contains(Path.DirectorySeparatorChar) ?? true ? rootPath : rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) ?? throw new ArgumentNullException(nameof(rootPath));
        }
        private readonly string ClientNamespaceName;
        private readonly string RootPath;
        public abstract string FileSuffix { get; }
        public abstract string NormalizeNamespaceSegment(string segmentName);
        public abstract string NormalizeFileName(string elementName);
        public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement) {
            var targetPath = Path.Combine(RootPath, 
                                            currentNamespace.Name
                                            .Replace(ClientNamespaceName, string.Empty)
                                            .TrimStart('.')
                                            .Split('.')
                                            .Select(x => NormalizeNamespaceSegment(x))
                                            .Aggregate((x, y) => $"{x}{Path.DirectorySeparatorChar}{y}"),
                                            NormalizeFileName(currentElement.Name) + FileSuffix);
            var directoryPath = Path.GetDirectoryName(targetPath);
            if(!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return targetPath;
        }
    }
}
