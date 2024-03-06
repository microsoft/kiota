using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class TypeScriptPathSegmenterTests
    {
        private readonly TypeScriptPathSegmenter segmenter;
        public TypeScriptPathSegmenterTests()
        {
            segmenter = new TypeScriptPathSegmenter("D:\\source\\repos\\kiota-sample", "client");
        }

        [Fact]
        public void TypeScriptPathSegmenterGeneratesCorrectFileName()
        {
            var rootNamespace = CodeNamespace.InitRootNamespace();
            var classExample = rootNamespace.AddClass(new CodeClass
            {
                Name = "testClass"
            }).First();
            var fileName = segmenter.NormalizeFileName(classExample);
            Assert.Equal("testClass", fileName);// the file name should be camelCase
        }

        [Fact]
        public void TypeScriptPathSegmenterGeneratesNamespaceFolderName()
        {
            var namespaceName = "MicrosoftGraph";
            var normalizedNamespace = segmenter.NormalizeNamespaceSegment(namespaceName);
            Assert.Equal("microsoftGraph", normalizedNamespace);// the namespace name should be camelCase
        }
    }
}
