using System;

namespace kiota.core {
    public interface IPathSegmenter {
        string GetPath(CodeNamespace currentNamespace, CodeClass currentClass);
        string FileSuffix { get; }
    }
}
