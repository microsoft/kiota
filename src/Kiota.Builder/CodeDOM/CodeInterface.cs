namespace Kiota.Builder;

public enum CodeInterfaceKind {
    Custom,
    Model,
}

public class CodeInterface : ProprietableBlock<CodeInterfaceKind>, ITypeDefinition
{
    public CodeInterface():base()
    {
        StartBlock = new Declaration() { Parent = this};
        EndBlock = new End() { Parent = this };
    }
    public class End : BlockEnd
    {
    }
    public class Declaration : ProprietableBlockDeclaration
    {
    }
}
