using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeClassDeclarationWriter : CSharp.CodeClassDeclarationWriter
    {
        public CodeClassDeclarationWriter(CSharpConventionService conventionService) : base(conventionService) { }
    }
}
