using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class RubyPathSegmenter : CommonPathSegmenter
{
    public RubyPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
    public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
    {
        return currentElement switch
        {
            CodeNamespace cn when !ClientNamespaceName.Equals(cn.Name, StringComparison.OrdinalIgnoreCase) => new[] { fileName },// We put barrels inside namespace folders
            _ => Enumerable.Empty<string>(),
        };
    }
    public override string FileSuffix => ".rb";
    private readonly ConcurrentDictionary<CodeNamespace, Dictionary<string, CodeElement[]>> collidingFileNames = new();
    /// <summary>
    /// Snake casing is lossy: names that differ only in where a separator falls, such as
    /// codeScanningVariantAnalysis_status and codeScanningVariantAnalysisStatus, collapse onto the
    /// same path. One model then silently overwrote the other and the barrel required it twice, so
    /// the second and later members of a colliding set get a numeric suffix. The ordering is by
    /// ordinal name, which keeps the result stable between the require path and the output path.
    /// </summary>
    public override string NormalizeFileName(CodeElement currentElement)
    {
        var fileName = GetLastFileNameSegment(currentElement).ToSnakeCase();
        if (currentElement is not (CodeClass or CodeEnum) || currentElement.Parent is not CodeNamespace parentNamespace)
            return fileName;
        var collisions = collidingFileNames.GetOrAdd(parentNamespace, static ns =>
            ns.Classes.Cast<CodeElement>()
                .Concat(ns.Enums)
                .GroupBy(static x => GetLastFileNameSegment(x).ToSnakeCase(), StringComparer.OrdinalIgnoreCase)
                .Where(static x => x.Skip(1).Any())
                .ToDictionary(static x => x.Key,
                              static x => x.OrderBy(static y => y.Name, StringComparer.Ordinal).ToArray(),
                              StringComparer.OrdinalIgnoreCase));
        if (!collisions.TryGetValue(fileName, out var siblings))
            return fileName;
        var index = Array.FindIndex(siblings, x => ReferenceEquals(x, currentElement));
        return index > 0 ? $"{fileName}_{index + 1}" : fileName;
    }
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToSnakeCase();
    public override string NormalizePath(string fullPath) =>
        ExceedsMaxPathLength(fullPath) && Path.GetDirectoryName(fullPath) is string directoryName ?
            Path.Combine(directoryName,
                        ShortenFileName(directoryName, Path.GetFileName(fullPath)) + FileSuffix) :
            fullPath;
    private string ShortenFileName(string directoryName, string currentFileName) =>
        currentFileName.Replace(FileSuffix, string.Empty, StringComparison.Ordinal)
                        .ShortenFileName(Math.Min(MaxFilePathLength - directoryName.Length, MaxFileNameLength));
    private const int MaxFilePathLength = 230;
    internal const int MaxFileNameLength = 98; // brute force tested
    public bool ExceedsMaxPathLength(string fullPath) =>
        !string.IsNullOrEmpty(fullPath) && (fullPath.Length - RootPath.Length) > MaxFilePathLength || Path.GetFileName(fullPath).Length > MaxFileNameLength;
    public string GetRelativeFileName(CodeNamespace currentNamespace, CodeElement currentElement) =>
        ExceedsMaxPathLength(GetPath(currentNamespace, currentElement, false)) ?
            Path.GetFileName(GetPath(currentNamespace, currentElement, true)).Replace(FileSuffix, string.Empty, StringComparison.Ordinal) :
            NormalizeFileName(currentElement);
}
