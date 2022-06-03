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
}
