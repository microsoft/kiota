using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder;

public enum CodeInterfaceKind
{
    Custom,
    Model,
    QueryParameters,
    RequestConfiguration
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind, InterfaceDeclaration>, ITypeDefinition
{
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{

    private List<CodeType> inherits = new List<CodeType>();
    public IEnumerable<CodeType> Inherits
    {
        get => inherits; set
        {
            inherits.Clear();
            inherits.AddRange(value);
        }
    }

    public void AddInheritsFrom(params CodeType[] inheritsFrom)
    {
        if (inheritsFrom == null || inheritsFrom.Any(x => x == null))
            throw new ArgumentNullException(nameof(inheritsFrom));
        if (!inheritsFrom.Any())
            throw new ArgumentOutOfRangeException(nameof(inheritsFrom));
        this.inherits.AddRange(inheritsFrom);
    }
}
