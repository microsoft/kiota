using System.ComponentModel.Design;

namespace Kiota.Builder;

public enum CodeInterfaceKind {
    Custom,
    Model,
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind, InterfaceDeclaration>, ITypeDefinition
{
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{
    public CodeType inherits;
}
