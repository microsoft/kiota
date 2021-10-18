using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Shell
{
    class ShellWriter : CSharpWriter
    {
        public ShellWriter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName)
        {
            var conventionService = new CSharpConventionService();
            AddCodeElementWriter(new ShellCodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter(conventionService));
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeIndexerWriter(conventionService));
            AddCodeElementWriter(new ShellCodeMethodWriter(conventionService));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
