using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

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
    public CodeClass OriginalMethodParentClass
    {
        get;
        private set;
    }
    private readonly ConcurrentDictionary<string, CodeTypeParameter> typeParameters = new(StringComparer.OrdinalIgnoreCase);
    public void AddTypeParameter(params CodeTypeParameter[] parameters)
    {
        if (parameters is null || parameters.Any(static x => x == null))
            throw new ArgumentNullException(nameof(parameters));
        EnsureElementsAreChildren(parameters);
        foreach (var parameter in parameters)
            typeParameters.TryAdd(parameter.Name, parameter);
    }
    public IReadOnlyList<CodeTypeParameter> TypeParameters => typeParameters.Values.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    public bool IsGeneric => !typeParameters.IsEmpty;
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
