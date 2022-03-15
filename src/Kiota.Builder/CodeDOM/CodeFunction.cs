using System;
using System.Linq;

namespace Kiota.Builder;

/// <summary>
/// Represents global functions for languages that support both local functions to classes and global functions like TypeScript for instance.
/// </summary>
public class CodeFunction : CodeBlock<BlockDeclaration, BlockEnd>
{
    public override string Name
    {
        get
        {
            return OriginalLocalMethod.Name;
        }
        set
        {
            OriginalLocalMethod.Name = value;
        }
    }
    public CodeMethod OriginalLocalMethod
    {
        get; private set;
    }
    public CodeFunction(CodeMethod method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (!method.IsStatic) throw new InvalidOperationException("The original method must be static");
        EnsureElementsAreChildren(method);
        OriginalLocalMethod = method;
    }
}
