using System.IO;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Xunit;

namespace Kiota.Builder.Tests.PathSegmenters
{
    public class HttpPathSegmenterTests
    {
        private readonly HttpPathSegmenter segmenter;

        public HttpPathSegmenterTests()
        {
            var tempFilePath = Path.GetTempFileName();
            segmenter = new HttpPathSegmenter(tempFilePath, "client");
        }

        [Fact]
        public void HttpPathSegmenterGeneratesCorrectFileName()
        {
            var fileName = segmenter.NormalizeFileName(new CodeClass
            {
                Name = "testClass"
            });
            Assert.Equal("TestClass", fileName);// the file name should be Proper case
        }

        [Fact]
        public void HttpPathSegmenterGeneratesNamespaceFolderName()
        {
            var namespaceName = "microsoft.Graph";
            var normalizedNamespace = segmenter.NormalizeNamespaceSegment(namespaceName);
            Assert.Equal("Microsoft.Graph", normalizedNamespace);// the first character is upper case
        }
    }
}
