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
#region common
        [Fact]
        public void ReplacesImportsSubNamespace() {
            var rootNS = parentClass.Parent as CodeNamespace;
            rootNS.RemoveChildElement(parentClass);
            graphNS.AddClass(parentClass);
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = graphNS.AddNamespace($"{graphNS.Name}.messages");
            var messageClassDef = new CodeClass {
                Name = "Message",
            };
            subNS.AddClass(messageClassDef);
            declaration.AddUsings(new CodeUsing {
                Name = messageClassDef.Name,
                Declaration = new() {
                    Name = messageClassDef.Name,
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("./messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsParentNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var subNS = root.AddNamespace("messages");
            var messageClassDef = new CodeClass {
                Name = "Message",
            };
            subNS.AddClass(messageClassDef);
            declaration.AddUsings(new CodeUsing() {
                Name = "messages",
                Declaration = new() {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript, ClientNamespaceName = string.Empty }, root);
            Assert.Equal("../messages/message", declaration.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsInOtherTrunk() {
            var usedRangeNS1 = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook.tables.worksheet.pivotTables.usedRange");
            var usedRangeNS2 = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook.worksheets.usedRange");
            var workbookNS = graphNS.AddNamespace($"{graphNS.Name}.workbooks.workbook");
            var workbookRangeClassDef = new CodeClass {
                Name = "workbookRange",
            };
            workbookNS.AddClass(workbookRangeClassDef);
            var usedRangeClassDef1 = new CodeClass {
                Name = "usedRangeRequestBuilder",
            };
            usedRangeNS1.AddClass(usedRangeClassDef1);
            
            var declaration1 = usedRangeClassDef1.StartBlock as CodeClass.Declaration;
            declaration1.AddUsings(new CodeUsing {
                Name = workbookNS.Name,
                Declaration = new () {
                    Name = workbookRangeClassDef.Name,
                    TypeDefinition = workbookRangeClassDef,
                }
            });
            var usedRangeClassDef2 = new CodeClass {
                Name = "usedRangeRequestBuilder",
            };
            usedRangeNS2.AddClass(usedRangeClassDef2);
            var declaration2 = usedRangeClassDef2.StartBlock as CodeClass.Declaration;
            declaration2.AddUsings(new CodeUsing {
                Name = workbookNS.Name,
                Declaration = new () {
                    Name = workbookRangeClassDef.Name,
                    TypeDefinition = workbookRangeClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript, ClientNamespaceName = graphNS.Name }, root);
            Assert.Equal("../../../../workbookRange", declaration1.Usings.First().Declaration.Name);
            Assert.Equal("../../workbookRange", declaration2.Usings.First().Declaration.Name);
        }
        [Fact]
        public void ReplacesImportsSameNamespace() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            var messageClassDef = new CodeClass {
                Name = "Message",
            };
            graphNS.AddClass(messageClassDef);
            declaration.AddUsings(new CodeUsing {
                Name = "graph",
                Declaration = new() {
                    Name = "Message",
                    TypeDefinition = messageClassDef,
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.TypeScript }, root);
            Assert.Equal("./message", declaration.Usings.First().Declaration.Name);
        }
#endregion
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
            var responseHandlerMethod = model.AddMethod(new CodeMethod {
                Name = "defaultResponseHandler",
                ReturnType = new CodeType {
                    Name = "string"
                }
            }, new () {
                Name = "deserializeFields",
                ReturnType = new CodeType() {
                    Name = DeserializeDefaultName,
                },
                MethodKind = CodeMethodKind.Deserializer
            }).First();
            const string streamDefaultName = "Stream";
            responseHandlerMethod.AddParameter(new CodeParameter {
                Name = "param1",
                Type = new CodeType {
                    Name = streamDefaultName
                }
            });
            ILanguageRefiner.Refine(new GenerationConfiguration{ Language = GenerationLanguage.TypeScript }, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => HttpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => FactoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => DateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => AddiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => DeserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => HandlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => streamDefaultName.Equals(x.Type.Name)));
        }
    }
#endregion
}
