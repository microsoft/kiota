namespace Kiota.Builder;

public enum CodeInterfaceKind {
    Custom,
    Model,
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind, InterfaceDeclaration>, ITypeDefinition
{
    public CodeClass OriginalClass { get; set; }
}
public class InterfaceDeclaration : ProprietableBlockDeclaration
{
}
