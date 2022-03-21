using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeEnumWriter : CSharp.CodeEnumWriter
    {
        public CodeEnumWriter(CSharpConventionService conventionService) : base(conventionService) { }
    }
}
