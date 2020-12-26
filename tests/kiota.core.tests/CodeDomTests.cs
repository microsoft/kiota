using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace kiota.core.tests
{
    public class CodeDomTests
    {
        [Fact]
        public void CreateClassAndRender()
        {
            var myNamespace = new CodeNamespace() {
                Name = "foo"
            };
            var myClass = new CodeClass() { Name = "bar"};
            myNamespace.AddClass(myClass);

            var outputCode = CodeRenderer.RenderCodeAsString(new CSharpWriter(),myNamespace);

            Assert.Equal(@"namespace foo {
    public class bar {
    }
}
", outputCode);
            
        }
    }
}
