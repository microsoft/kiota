using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class JavaLanguageRefinerTests {
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
        #region CommonLanguageRefinerTests
        [Fact]
        public void AddsDefaultImports() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var requestBuilder = root.AddClass(new CodeClass(root) {
                Name = "rb",
                ClassKind = CodeClassKind.RequestBuilder,
            }).First();
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEmpty(model.StartBlock.Usings);
            Assert.NotEmpty(requestBuilder.StartBlock.Usings);
        }
        [Fact]
        public void ReplacesBinaryByNativeType() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var method = model.AddMethod(new CodeMethod(model) {
                Name = "method"
            }).First();
            method.ReturnType = new CodeType(method) {
                Name = "binary"
            };
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEmpty(model.StartBlock.Usings);
            Assert.NotEqual("binary", method.ReturnType.Name);
        }
        [Fact]
        public void ConvertsDeserializerPropsToMethods() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var property = model.AddProperty(new CodeProperty(model) {
                Name = "deserialize",
                PropertyKind = CodePropertyKind.Deserializer,
            }).First();
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.Empty(model.GetChildElements().OfType<CodeProperty>());
            Assert.NotEmpty(model.GetChildElements().OfType<CodeMethod>());
        }
        [Fact]
        public void ReplacesIndexersByMethodsWithParameter() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var requestBuilder = root.AddClass(new CodeClass (root) {
                Name = "requestBuilder",
                ClassKind = CodeClassKind.Model
            }).First();
            requestBuilder.AddProperty(new CodeProperty(requestBuilder) {
                Name = "pathSegment",
                DefaultValue = "path",
                Type = new CodeType(requestBuilder) {
                    Name = "string",
                }
            });
            requestBuilder.SetIndexer(new CodeIndexer(requestBuilder) {
                Name = "idx",
                ReturnType = new CodeType(requestBuilder) {
                    Name = "model",
                    TypeDefinition = model,
                },
            });
            var collectionRequestBuilder = root.AddClass(new CodeClass(root) {
                Name = "CollectionRequestBUilder",
            }).First();
            collectionRequestBuilder.AddProperty(new CodeProperty(collectionRequestBuilder) {
                Name = "collection",
                Type = new CodeType(requestBuilder) {
                    Name = "requestBuilder",
                    TypeDefinition = requestBuilder,
                },
            });
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.Single(requestBuilder.GetChildElements(true));
            Assert.True(requestBuilder.GetChildElements(true).First() is CodeProperty);
            Assert.Equal(2, collectionRequestBuilder.GetChildElements(true).Count());
            Assert.Single(collectionRequestBuilder.GetChildElements(true).OfType<CodeMethod>());
            Assert.Single(collectionRequestBuilder.GetChildElements(true).OfType<CodeProperty>());
        }
        #endregion
    }
}
