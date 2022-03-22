using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeBlockEndWriter : CSharp.CodeBlockEndWriter
    {
        public CodeBlockEndWriter(CSharpConventionService conventionService) : base(conventionService) { }
    }
}
