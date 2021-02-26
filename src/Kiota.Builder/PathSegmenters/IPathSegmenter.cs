using System;

namespace Kiota.Builder {
    public interface IPathSegmenter {
        string GetPath(CodeNamespace currentNamespace, CodeClass currentClass);
        string FileSuffix { get; }
    }
}
