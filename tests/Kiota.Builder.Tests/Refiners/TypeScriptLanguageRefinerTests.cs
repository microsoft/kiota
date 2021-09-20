using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class TypeScriptLanguageRefinerTests {
        private readonly CodeNamespace root;
        private readonly CodeNamespace graphNS;
        private readonly CodeClass parentClass;
        public TypeScriptLanguageRefinerTests() {
            root = CodeNamespace.InitRootNamespace();
            graphNS = root.AddNamespace("graph");
            parentClass = new () {
                Name = "parentClass"
            };
            graphNS.AddClass(parentClass);
        }
#region typescript
        private const string HttpCoreDefaultName = "IHttpCore";
        private const string FactoryDefaultName = "ISerializationWriterFactory";
        private const string DeserializeDefaultName = "IDictionary<string, Action<Model, IParseNode>>";
        private const string DateTimeOffsetDefaultName = "DateTimeOffset";
        private const string AddiationalDataDefaultName = "new Dictionary<string, object>()";
        private const string HandlerDefaultName = "IResponseHandler";
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.NotEqual("break", model.Name);
            Assert.Contains("escaped", model.Name);
        }
        [Fact]
        public void CorrectsCoreType() {

            var model = root.AddClass(new CodeClass () {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new CodeProperty() {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
                Type = new CodeType {
                    Name = HttpCoreDefaultName
                }
            }, new () {
                Name = "someDate",
                PropertyKind = CodePropertyKind.Custom,
                Type = new CodeType {
                    Name = DateTimeOffsetDefaultName,
                }
            }, new () {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
                Type = new CodeType {
                    Name = AddiationalDataDefaultName
                }
            });
            var executorMethod = model.AddMethod(new CodeMethod {
                Name = "executor",
                MethodKind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType {
                    Name = "string"
                }
            }).First();
            executorMethod.AddParameter(new CodeParameter {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType {
                    Name = HandlerDefaultName,
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
            ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.TypeScript }, root);
            Assert.Empty(model.Properties.Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => FactoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Properties.Where(x => AddiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Methods.Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.Methods.SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        }
    }
#endregion
}
