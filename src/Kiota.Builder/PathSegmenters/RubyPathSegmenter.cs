using System;
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
    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();
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
