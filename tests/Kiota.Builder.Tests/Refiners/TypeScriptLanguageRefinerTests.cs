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
        [Fact]
        public void AliasesDuplicateUsingSymbols() {
            var model = graphNS.AddClass(new CodeClass {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var modelsNS = graphNS.AddNamespace($"{graphNS.Name}.models");
            var source1 = modelsNS.AddClass(new CodeClass {
                Name = "source",
                ClassKind = CodeClassKind.Model
            }).First();
            var submodelsNS = modelsNS.AddNamespace($"{modelsNS.Name}.submodels");
            var source2 = submodelsNS.AddClass(new CodeClass {
                Name = "source",
                ClassKind = CodeClassKind.Model
            }).First();

            var using1 = new CodeUsing {
               Name = modelsNS.Name,
               Declaration = new CodeType {
                   Name = source1.Name,
                   TypeDefinition = source1,
                   IsExternal = false,
               }
            };
            var using2 = new CodeUsing {
               Name = submodelsNS.Name,
               Declaration = new CodeType {
                   Name = source2.Name,
                   TypeDefinition = source2,
                   IsExternal = false,
               }
            };
            model.AddUsing(using1);
            model.AddUsing(using2);
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.NotEmpty(using1.Alias);
            Assert.NotEmpty(using2.Alias);
            Assert.NotEqual(using1.Alias, using2.Alias);
        }
#endregion
    }
}
