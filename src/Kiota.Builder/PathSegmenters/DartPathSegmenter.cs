using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.PathSegmenters;

public class DartPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".dart";

    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToCamelCase();

    public override string NormalizeFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToSnakeCase();

    internal string GetRelativeFileName(CodeNamespace @namespace, CodeElement element)
    {
        return NormalizeFileName(element);
    }
}
