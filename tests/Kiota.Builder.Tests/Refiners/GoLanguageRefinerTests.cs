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
            var model = root.AddClass(new CodeClass {
                Name = "someModel",
                ClassKind = CodeClassKind.RequestBuilder
            }).First();
            var rb = model.AddProperty(new CodeProperty {
                Name = "someProperty",
                PropertyKind = CodePropertyKind.RequestBuilder,
            }).First();
            rb.Type = new CodeType {
                Name = "someType",
            };
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Empty(model.Properties);
            Assert.Single(model.Methods.Where(x => x.IsOfKind(CodeMethodKind.RequestBuilderBackwardCompatibility)));
        }
        [Fact]
        public void AddsErrorImportForEnums() {
            var testEnum = root.AddEnum(new CodeEnum {
                Name = "TestEnum",

            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Single(testEnum.Usings);
        }
        [Fact]
        public void CorrectsCoreType() {
            const string requestAdapterDefaultName = "IRequestAdapter";
            const string factoryDefaultName = "ISerializationWriterFactory";
            const string deserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
            const string dateTimeOffsetDefaultName = "DateTimeOffset";
            const string addiationalDataDefaultName = "new Dictionary<string, object>()";
            var model = root.AddClass(new CodeClass {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new () {
                Name = "core",
                PropertyKind = CodePropertyKind.RequestAdapter,
                Type = new CodeType {
                    Name = requestAdapterDefaultName
                }
            }, new () {
                Name = "someDate",
                PropertyKind = CodePropertyKind.Custom,
                Type = new CodeType {
                    Name = dateTimeOffsetDefaultName,
                }
            }, new () {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
                Type = new CodeType {
                    Name = addiationalDataDefaultName
                }
            });
            const string handlerDefaultName = "IResponseHandler";
            const string headersDefaultName = "IDictionary<string, string>";
            var executorMethod = model.AddMethod(new CodeMethod {
                Name = "executor",
                MethodKind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType {
                    Name = "string"
                }
            }, new () {
                Name = "deserializeFields",
                ReturnType = new CodeType {
                    Name = deserializeDefaultName,
                },
                MethodKind = CodeMethodKind.Deserializer
            }).First();
            executorMethod.AddParameter(new () {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType {
                    Name = handlerDefaultName,
                }
            }, new () {
                Name = "headers",
                ParameterKind = CodeParameterKind.Headers,
                Type = new CodeType {
                    Name = headersDefaultName
                }
            });
            const string serializerDefaultName = "ISerializationWriter";
            var serializationMethod = model.AddMethod(new CodeMethod {
                Name = "seriailization",
                MethodKind = CodeMethodKind.Serializer,
                ReturnType = new CodeType {
                    Name = "string"
                }
            }).First();
            serializationMethod.AddParameter(new CodeParameter {
                Name = "handler",
                ParameterKind = CodeParameterKind.Serializer,
                Type = new CodeType {
                    Name = serializerDefaultName,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Go }, root);
            Assert.Empty(model.Properties.Where(x => requestAdapterDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => factoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Methods.Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        }
        #endregion
    }
}
