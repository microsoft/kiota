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
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        }
       
    }
}
