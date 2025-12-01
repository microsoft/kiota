using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.PathSegmenters;

public abstract class CommonPathSegmenter : IPathSegmenter
{
    protected CommonPathSegmenter(string rootPath, string clientNamespaceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);
        ArgumentException.ThrowIfNullOrEmpty(clientNamespaceName);
        ClientNamespaceName = clientNamespaceName;
        RootPath = rootPath.Contains(Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? rootPath : rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
    protected string ClientNamespaceName
    {
        get; init;
    }
    protected string RootPath
    {
        get; init;
    }
    public abstract string FileSuffix
    {
        get;
    }
    public abstract string NormalizeNamespaceSegment(string segmentName);
    public abstract string NormalizeFileName(CodeElement currentElement);
    public virtual string NormalizePath(string fullPath) => fullPath;
    public virtual IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName) => Enumerable.Empty<string>();
    protected static string GetLastFileNameSegment(CodeElement currentElement) => currentElement?.Name.Split('.')[^1] ?? string.Empty;
    public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true)
    {
        ArgumentNullException.ThrowIfNull(currentNamespace);
        var fileName = NormalizeFileName(currentElement);
        var namespacePathSegments = new List<string>(currentNamespace.Name
                                        .Replace(ClientNamespaceName, string.Empty, StringComparison.Ordinal)
                                        .TrimStart('.')
                                        .Split('.'));
        namespacePathSegments.AddRange(GetAdditionalSegment(currentElement, fileName)); //Union removes duplicates so we're building a list instead to conserve those.
        namespacePathSegments = namespacePathSegments.Where(x => !string.IsNullOrEmpty(x))
                                        .Select(NormalizeNamespaceSegment)
                                        .ToList();
        var targetPath = Path.Combine(RootPath, namespacePathSegments.Count != 0 ? namespacePathSegments
                                        .Aggregate(static (x, y) => $"{x}{Path.DirectorySeparatorChar}{y}") : string.Empty,
                                        fileName + FileSuffix);
        if (shouldNormalizePath)
            targetPath = NormalizePath(targetPath);
        var directoryPath = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);
        return targetPath;
    }
}
