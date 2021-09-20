using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class GoLanguageRefinerTests {
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
        #region CommonLangRefinerTests

        #endregion

        #region GoRefinerTests
        [Fact]
        public void ReplacesRequestBuilderPropertiesByMethods() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "someModel",
                ClassKind = CodeClassKind.RequestBuilder
            }).First();
            var rb = model.AddProperty(new CodeProperty(model) {
                Name = "someProperty",
                PropertyKind = CodePropertyKind.RequestBuilder,
            }).First();
            rb.Type = new CodeType(rb) {
                Name = "someType",
            };
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>());
            Assert.Single(model.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility)));
        }
        [Fact]
        public void AddsErrorImportForEnums() {
            var testEnum = root.AddEnum(new CodeEnum(root) {
                Name = "TestEnum",

            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Single(testEnum.Usings);
        }
        [Fact]
        public void MovesModelsInDedicatedNamespace() {
            var main = root.AddNamespace("/main");
            main.AddClass(new CodeClass (main) {
                Name = "someModel",
                ClassKind = CodeClassKind.Model
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Empty(main.GetChildElements(true).OfType<CodeClass>());
            var modelsNS = main.FindNamespaceByName("/main.models");
            Assert.NotNull(modelsNS);
            Assert.Single(modelsNS.GetChildElements(true).OfType<CodeClass>());
        }
        [Fact]
        public void CorrectsCoreType() {
            const string httpCoreDefaultName = "IHttpCore";
            const string factoryDefaultName = "ISerializationWriterFactory";
            const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
            const string dateTimeOffsetDefaultName = "DateTimeOffset";
            const string addiationalDataDefaultName = "new Dictionary<string, object>()";
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new (model) {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
                Type = new CodeType(model) {
                    Name = httpCoreDefaultName
                }
            }, new (model) {
                Name = "someDate",
                PropertyKind = CodePropertyKind.Custom,
                Type = new CodeType(model) {
                    Name = dateTimeOffsetDefaultName,
                }
            }, new (model) {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
                Type = new CodeType(model) {
                    Name = addiationalDataDefaultName
                }
            });
            const string handlerDefaultName = "IResponseHandler";
            const string headersDefaultName = "IDictionary<string, string>";
            var executorMethod = model.AddMethod(new CodeMethod(model) {
                Name = "executor",
                MethodKind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType(model) {
                    Name = "string"
                }
            }, new (model) {
                Name = "deserializeFields",
                ReturnType = new CodeType(model) {
                    Name = deserializeDefaultName,
                },
                MethodKind = CodeMethodKind.Deserializer
            }).First();
            executorMethod.AddParameter(new (executorMethod) {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType(executorMethod) {
                    Name = handlerDefaultName,
                }
            }, new (executorMethod) {
                Name = "headers",
                ParameterKind = CodeParameterKind.Headers,
                Type = new CodeType(executorMethod) {
                    Name = headersDefaultName
                }
            });
            const string serializerDefaultName = "ISerializationWriter";
            var serializationMethod = model.AddMethod(new CodeMethod(model) {
                Name = "seriailization",
                MethodKind = CodeMethodKind.Serializer,
                ReturnType = new CodeType(model) {
                    Name = "string"
                }
            }).First();
            serializationMethod.AddParameter(new CodeParameter(serializationMethod) {
                Name = "handler",
                ParameterKind = CodeParameterKind.Serializer,
                Type = new CodeType(executorMethod) {
                    Name = serializerDefaultName,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => httpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => factoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        }
        #endregion
    }
}
