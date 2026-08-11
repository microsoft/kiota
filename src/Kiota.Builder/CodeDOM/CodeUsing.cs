using System;

namespace Kiota.Builder.CodeDOM;

public class CodeUsing : CodeElement, ICloneable
{
    private CodeType? declaration;
    public CodeType? Declaration
    {
        get => declaration; set
        {
            EnsureElementsAreChildren(value);
            declaration = value;
        }
    }
    public bool IsExternal
    {
        get => Declaration?.IsExternal ?? true;
    }
    public bool IsErasable
    {
        get; set;
    }
    public string Alias { get; set; } = string.Empty;
    public object Clone()
    {
        return new CodeUsing
        {
            Declaration = Declaration?.Clone() as CodeType,
            Alias = Alias,
            Name = Name,
            Parent = Parent,
            IsErasable = IsErasable,
        };
    }
}
