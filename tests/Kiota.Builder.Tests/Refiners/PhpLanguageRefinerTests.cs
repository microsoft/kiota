using System.Linq;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Php;
using Xunit;

namespace Kiota.Builder.Refiners.Tests
{
    public class PhpLanguageRefinerTests
    {
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

        [Fact]
        public void ReplacesRequestBuilderPropertiesByMethods()
        {
            var model = root.AddClass(new CodeClass()
            {
                Name = "userRequestBuilder",
                ClassKind = CodeClassKind.RequestBuilder
            }).First();

            var requestBuilder = model.AddProperty(new CodeProperty()
            {
                Name = "breaks", 
                PropertyKind = CodePropertyKind.RequestBuilder,
                Type = new CodeType()
                {
                    Name = "string"
                }
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, root);
            Assert.Equal("breaks", requestBuilder.Name);
            Assert.Equal("userRequestBuilder", model.Name);
        }

        [Fact]
        public void PrefixReservedWordPropertyNamesWith()
        {
            var model = root.AddClass(new CodeClass()
            {
                Name = "userRequestBuilder",
                ClassKind = CodeClassKind.RequestBuilder
            }).First();

            var property = model.AddProperty(new CodeProperty()
            {
                Name = "continue", 
                PropertyKind = CodePropertyKind.RequestBuilder,
                Type = new CodeType()
                {
                    Name = "string"
                }
            }).First();
            
            ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, root);
            Assert.Equal("GraphContinue",property.Name);
        }
        
        [Fact]
        public void ReplacesBinaryWithNativeType()
        {
            var model = root.AddClass(new CodeClass () {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var method = model.AddMethod(new CodeMethod() {
                Name = "method"
            }).First();
            method.ReturnType = new CodeType() {
                Name = "binary"
            };
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP}, root);
            Assert.Equal("StreamInterface", method.ReturnType.Name);
        }

        [Fact]
        public void AddsDefaultImports() {
            var model = root.AddClass(new CodeClass () {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var requestBuilder = root.AddClass(new CodeClass() {
                Name = "rb",
                ClassKind = CodeClassKind.RequestBuilder,
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.PHP }, root);
            Assert.NotEmpty(model.StartBlock.Usings);
        }

        [Fact]
        public void TestCanReturnCorrectAccess()
        {
            
        }
    }
}
