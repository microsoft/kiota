using System;

namespace Kiota.Builder {
    public interface IPathSegmenter {
        string GetPath(CodeNamespace currentNamespace, CodeElement currentElement);
        string GetPath(CodeElement currentElement, string outputFolder);
    }
}
