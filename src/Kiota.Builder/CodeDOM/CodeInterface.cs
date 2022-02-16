namespace Kiota.Builder;

public enum CodeInterfaceKind {
    Custom,
    Model,
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind>, ITypeDefinition
{
    public CodeInterface():base()
    {
        StartBlock = new InterfaceDeclaration() { Parent = this};
        EndBlock = new InterfaceEnd() { Parent = this };
    }
    public class InterfaceEnd : BlockEnd
    {
    }
    public class InterfaceDeclaration : ProprietableBlockDeclaration
    {
    }
}
