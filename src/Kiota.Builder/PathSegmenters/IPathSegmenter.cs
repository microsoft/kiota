using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.PathSegmenters;

public interface IPathSegmenter
{
    string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true);
}
