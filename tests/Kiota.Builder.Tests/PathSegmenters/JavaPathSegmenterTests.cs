using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class JavaPathSegmenterTests
    {
        private readonly JavaPathSegmenter segmenter;
        public JavaPathSegmenterTests()
        {
            segmenter = new JavaPathSegmenter("D:\\source\\repos\\kiota-sample", "client");
        }

        [Fact]
        public void JavaPathSegmenterGeneratesCorrectFileName()
        {
            var fileName = segmenter.NormalizeFileName(new CodeClass
            {
                Name = "testClass"
            });
            Assert.Equal("TestClass", fileName);// the file name should be PascalCase
        }

        [Fact]
        public void JavaPathSegmenterGeneratesNamespaceFolderName()
        {
            var namespaceName = "Microsoft.Graph";
            var normalizedNamespace = segmenter.NormalizeNamespaceSegment(namespaceName);
            Assert.Equal("microsoft.graph", normalizedNamespace);// the file name should be lower case
        }
    }
}
