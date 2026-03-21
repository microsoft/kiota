using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class RustPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".rs";

    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToSnakeCase();

    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();
}
