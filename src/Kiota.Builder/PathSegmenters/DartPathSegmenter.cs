using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class DartPathSegmenter : CommonPathSegmenter
{
    public DartPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }

    public override string FileSuffix => ".dart";

    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToCamelCase();

    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();
}
