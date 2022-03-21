using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiota.Builder.Writers.PowerShell
{
    public class PowerShellWritter : LanguageWriter
    {
        public PowerShellWritter(string rootPath, string clientNamespaceName)
        {
            PathSegmenter = new PowerShellPathSegmenter(rootPath, clientNamespaceName);
            var conventionService = new PowerShellConventionService();
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter(conventionService));
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeIndexerWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
