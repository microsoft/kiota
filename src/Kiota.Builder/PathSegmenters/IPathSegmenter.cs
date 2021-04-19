using System;

namespace Kiota.Builder {
    public interface IPathSegmenter {
        string GetPath(CodeNamespace currentNamespace, CodeElement currentElement);
        string FileSuffix { get; }
    }
}
