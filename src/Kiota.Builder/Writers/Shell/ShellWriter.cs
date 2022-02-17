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
            AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
            AddOrReplaceCodeElementWriter(new ShellCodeMethodWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
            AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
