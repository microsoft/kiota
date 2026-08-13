using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class JavaPathSegmenter : CommonPathSegmenter
{
    public JavaPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
    public override string FileSuffix => ".java";
    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName?.ToLowerInvariant() ?? string.Empty;
}
