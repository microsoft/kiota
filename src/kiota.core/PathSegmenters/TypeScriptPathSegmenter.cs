using System;
using System.IO;
using System.Linq;

namespace kiota.core {
    public class TypeScriptPathSegmenter : IPathSegmenter
    {
        public TypeScriptPathSegmenter(string rootPath, string clientNamespaceName)
        {
            ClientNamespaceName = clientNamespaceName ?? throw new ArgumentNullException(nameof(clientNamespaceName));
            RootPath = (rootPath?.Contains(Path.DirectorySeparatorChar) ?? true ? rootPath : rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)) ?? throw new ArgumentNullException(nameof(rootPath));
        }
        private string ClientNamespaceName;
        private string RootPath;

        public string FileSuffix => ".ts";
        public string GetPath(CodeNamespace currentNamespace, CodeClass currentClass)
        {
            var targetPath = Path.Combine(RootPath, 
                                            currentNamespace.Name
                                            .Replace(ClientNamespaceName, string.Empty)
                                            .TrimStart('.')
                                            .Split('.')
                                            .Select(x => x.ToFirstCharacterLowerCase())
                                            .Aggregate((x, y) => $"{x}{Path.DirectorySeparatorChar}{y}"),
                                            currentClass.Name.ToFirstCharacterLowerCase() + FileSuffix);
            var directoryPath = Path.GetDirectoryName(targetPath);
            if(!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return targetPath;
        }
    }
}
