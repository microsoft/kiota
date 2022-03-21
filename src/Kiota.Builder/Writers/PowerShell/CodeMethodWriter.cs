using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeMethodWriter : CSharp.CodeMethodWriter
    {
        public CodeMethodWriter(CSharpConventionService conventionService) : base(conventionService) { }
    }
}
