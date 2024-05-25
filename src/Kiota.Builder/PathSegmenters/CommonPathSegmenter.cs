﻿using System;
using System.Collections.Generic;
using System.IO;

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
    public virtual IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName) => [];
    protected static string GetLastFileNameSegment(CodeElement currentElement) => currentElement?.Name.Split('.')[^1] ?? string.Empty;
    public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true)
    {
        ArgumentNullException.ThrowIfNull(currentNamespace);
        var fileName = NormalizeFileName(currentElement);
        var namespacePathSegments = new List<string>(currentNamespace.Name
                                        .Replace(ClientNamespaceName, string.Empty, StringComparison.Ordinal)
                                        .TrimStart('.')
                                        .Split('.'));
        namespacePathSegments.AddRange(GetAdditionalSegment(currentElement, fileName));

        var normalizedSegments = new List<string>();
        foreach (var segment in namespacePathSegments)
        {
            if (!string.IsNullOrEmpty(segment))
            {
                normalizedSegments.Add(NormalizeNamespaceSegment(segment));
            }
        }

        string aggregatedPath = string.Empty;
        foreach (var segment in normalizedSegments)
        {
            aggregatedPath += $"{Path.DirectorySeparatorChar}{segment}";
        }
        if (aggregatedPath.StartsWith(Path.DirectorySeparatorChar))
        {
            aggregatedPath = aggregatedPath.Substring(1);
        }

        var targetPath = Path.Combine(RootPath, normalizedSegments.Count != 0 ? aggregatedPath : string.Empty, fileName + FileSuffix);
        if (shouldNormalizePath)
            targetPath = NormalizePath(targetPath);
        var directoryPath = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);
        return targetPath;
    }
}
