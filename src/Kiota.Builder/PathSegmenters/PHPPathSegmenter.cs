using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;
public class PhpPathSegmenter : CommonPathSegmenter
{
    public PhpPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
    public override string FileSuffix => ".php";
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
    protected static new string GetLastFileNameSegment(CodeElement currentElement) => currentElement?.Name.Split(new char[] { '.', '\\' }, StringSplitOptions.RemoveEmptyEntries).Last() ?? string.Empty;
    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
}
