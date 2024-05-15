using System;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// Represents global functions for languages that support both local functions to classes and global functions like TypeScript for instance.
/// </summary>
public class CodeFunction : CodeBlock<BlockDeclaration, BlockEnd>
{
    [JsonIgnore]
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
    [JsonIgnore]
    public CodeClass OriginalMethodParentClass
    {
        get;
        private set;
    }
    public CodeFunction(CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (!method.IsStatic) throw new InvalidOperationException("The original method must be static");
        if (method.Parent is CodeClass parentClass)
            OriginalMethodParentClass = parentClass;
        else
            throw new InvalidOperationException("The original method must be a member of a class");
        EnsureElementsAreChildren(method);
        OriginalLocalMethod = method;
    }
}
