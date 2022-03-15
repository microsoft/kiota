using System.Collections.Generic;

namespace Kiota.Builder;

/// <summary>
/// Marker interface used for type searching.
/// </summary>
public interface IBlock {
    T FindChildByName<T>(string childName, bool findInChildElements = true) where T: ICodeElement;
    IEnumerable<T> FindChildrenByName<T>(string childName) where T: ICodeElement;
    void AddUsing(params CodeUsing[] codeUsings);
    CodeElement Parent { get; set; }
    IEnumerable<CodeUsing> Usings { get; }
}
