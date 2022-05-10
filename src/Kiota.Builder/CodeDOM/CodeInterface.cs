using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Kiota.Builder;

public enum CodeInterfaceKind {
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
    public List<CodeType> inherits = new List<CodeType>();
}
