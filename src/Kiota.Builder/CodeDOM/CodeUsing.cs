using System;

namespace Kiota.Builder;
public class CodeUsing : CodeElement, ICloneable
{
    private CodeType declaration;
    public CodeType Declaration { get => declaration; set {
        EnsureElementsAreChildren(declaration);
        declaration = value;
    } }
    public bool IsExternal {
        get => Declaration?.IsExternal ?? true;
    }
    public string Alias { get; set; }
    public object Clone()
    {
        return new CodeUsing {
            Declaration = (CodeType)Declaration?.Clone(),
            Alias = Alias,
            Name = Name.Clone() as string,
            Parent = Parent,
        };
    }
}
