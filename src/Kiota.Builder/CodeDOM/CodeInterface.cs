using System.Collections.Generic;

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
    public List<CodeType> Inherits
    {
        get => inherits; set
        {
            inherits = value;
        }
    }
}
