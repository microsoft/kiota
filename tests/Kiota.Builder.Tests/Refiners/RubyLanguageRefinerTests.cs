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
            parentClass = new () {
                Name = "parentClass"
            };
            graphNS.AddClass(parentClass);
        }
        #region CommonLanguageRefinerTests
        [Fact]
        public void DoesNotKeepCancellationParametersInRequestExecutors()
        {
            var model = root.AddClass(new CodeClass
            {
                Name = "model",
                Kind = CodeClassKind.RequestBuilder
            }).First();
            var method = model.AddMethod(new CodeMethod
            {
                Name = "getMethod",
                Kind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType
                {
                    Name = "string"
                }
            }).First();
            var cancellationParam = new CodeParameter
            {
                Name = "cancelletionToken",
                Optional = true,
                Kind = CodeParameterKind.Cancellation,
                Description = "Cancellation token to use when cancelling requests",
                Type = new CodeType { Name = "CancelletionToken", IsExternal = true },
            };
            method.AddParameter(cancellationParam);
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby }, root); //using CSharp so the cancelletionToken doesn't get removed
            Assert.False(method.Parameters.Any());
            Assert.DoesNotContain(cancellationParam, method.Parameters);
        }
        [Fact]
        public void AddsDefaultImports() {
            var model = root.AddClass(new CodeClass {
                Name = "model",
                Kind = CodeClassKind.Model
            }).First();
            var requestBuilder = root.AddClass(new CodeClass {
                Name = "rb",
                Kind = CodeClassKind.RequestBuilder,
            }).First();
            requestBuilder.AddMethod(new CodeMethod {
                Name = "get",
                Kind = CodeMethodKind.RequestExecutor,
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.NotEmpty(model.StartBlock.Usings);
            Assert.NotEmpty(requestBuilder.StartBlock.Usings);
        }
        #endregion
        #region RubyLanguageRefinerTests
        [Fact]
        public void CorrectsCoreTypes() {
            var model = root.AddClass(new CodeClass {
                Name = "rb",
                Kind = CodeClassKind.RequestBuilder
            }).First();
            var property = model.AddProperty(new CodeProperty {
                Name = "name",
                Type = new CodeType {
                    Name = "string",
                    IsExternal = true
                },
                Kind = CodePropertyKind.PathParameters,
                DefaultValue = "wrongDefaultValue"
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("Hash.new", property.DefaultValue);
        }
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass {
                Name = "break",
                Kind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.NotEqual("break", model.Name);
            Assert.Contains("escaped", model.Name);
        }
        [Fact]
        public void AddInheritedAndMethodTypesImports() {
            var model = root.AddClass(new CodeClass {
                Name = "model",
                Kind = CodeClassKind.Model
            }).First();
            var declaration = model.StartBlock as CodeClass.ClassDeclaration;
            declaration.Inherits = new (){
                Name = "someInterface"
            };
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("someInterface", declaration.Usings.First(usingDef => usingDef.Declaration != null).Declaration?.Name);
        }
        [Fact]
        public void FixInheritedEntityType() {
            var model = root.AddClass(new CodeClass {
                Name = "model",
                Kind = CodeClassKind.Model
            }).First();
            var entity = graphNS.AddClass(new CodeClass {
                Name = "entity",
                Kind = CodeClassKind.Model
            }).First();
            var declaration = model.StartBlock as CodeClass.ClassDeclaration;
            declaration.Inherits = new (){
                Name = "entity"
            };
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("Graph::Entity", declaration.Inherits.Name);
        }
        [Fact]
        public void AddNamespaceModuleImports() {
            var declaration = parentClass.StartBlock as CodeClass.ClassDeclaration;
            var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
            var messageClassDef = new CodeClass {
                Name = "Message",
            };
            subNS.AddClass(messageClassDef);
            declaration.AddUsings(new CodeUsing() {
                Name = messageClassDef.Name,
                Declaration = new() {
                    Name = messageClassDef.Name,
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Ruby, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("Message", declaration.Usings.First().Declaration.Name);
            Assert.Equal("./graph", declaration.Usings.Last().Declaration.Name);
        }
        #endregion
    }
}
