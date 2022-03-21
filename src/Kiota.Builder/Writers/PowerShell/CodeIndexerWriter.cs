using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeIndexerWriter : CSharp.CodeIndexerWriter
    {
        public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) { }
    }
}
