using System.IO;
using Xunit;

namespace kiota.core.tests
{
    public class CodeDomTests
    {
        [Fact]
        public void CreateClassAndRender()
        {
            var myNamespace = new CodeNamespace(null) {
                Name = "foo"
            };
            var myClass = new CodeClass(myNamespace) { Name = "bar"};
            myNamespace.AddClass(myClass);

            var outputCode = CodeRenderer.RenderCodeAsString(new CSharpWriter(Path.GetRandomFileName(), "foo"),myNamespace);

            Assert.Equal(@"namespace foo {
    public class bar {
    }
}
", outputCode);
            
        }
    }
}
