using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class RubyLanguageRefinerTests {

        private readonly CodeNamespace graphNS;
        private readonly CodeClass parentClass;
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
        public RubyLanguageRefinerTests() {
            root = CodeNamespace.InitRootNamespace();
            graphNS = root.AddNamespace("graph");
            parentClass = new (graphNS) {
                Name = "parentClass"
            };
            graphNS.AddClass(parentClass);
        }
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.NotEmpty(model.StartBlock.Usings);
            Assert.NotEmpty(requestBuilder.StartBlock.Usings);
        }
        [Fact]
        public void ReplacesImportsSubNamespace() {
            var rootNS = parentClass.Parent as CodeNamespace;
            rootNS.RemoveChildElement(parentClass);
            graphNS.AddClass(parentClass);
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
            var messageClassDef = new CodeClass(subNS) {
                Name = "Message",
            };
            declaration.Usings.Add(new (parentClass) {
                Name = "graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.Equal("./messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsParentNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = root.AddNamespace("messages");
            var messageClassDef = new CodeClass(subNS) {
                Name = "Message",
            };
            subNS.AddClass(messageClassDef);
            declaration.Usings.Add(new (parentClass) {
                Name = "messages",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.Equal("../messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsSameNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var messageClassDef = new CodeClass(graphNS) {
                Name = "Message",
            };
            declaration.Usings.Add(new (parentClass) {
                Name = "graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.Equal("./message", declaration.Usings.First().Declaration.Name);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.Single(requestBuilder.GetChildElements(true).OfType<CodeProperty>());
            Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
            Assert.Single(collectionRequestBuilder.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)));
            Assert.Single(collectionRequestBuilder.GetChildElements(true).OfType<CodeProperty>());
        }
        #endregion
        #region RubyLanguageRefinerTests
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root);
            Assert.NotEqual("break", model.Name);
            Assert.Contains("escaped", model.Name);
        }
        #endregion
    }
}
