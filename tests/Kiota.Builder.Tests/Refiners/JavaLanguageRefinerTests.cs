using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class JavaLanguageRefinerTests {
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
        #region CommonLanguageRefinerTests
        [Fact]
        public void EscapesReservedKeywordsInInternalDeclaration() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            var nUsing = new CodeUsing(model) {
                Name = "some.ns",
            };
            nUsing.Declaration = new CodeType(nUsing) {
                Name = "break",
                IsExternal = false,
            };
            model.AddUsing(nUsing);
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEqual("break", nUsing.Declaration.Name);
            Assert.Contains("escaped", nUsing.Declaration.Name);
        }
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEqual("break", model.Name);
            Assert.Contains("escaped", model.Name);
        }
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
        [Fact]
        public void AddsInnerClasses() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var method = model.AddMethod(new CodeMethod(model) {
                Name = "method1",
                ReturnType = new CodeType(model) {
                    Name = "string",
                    IsExternal = true
                }
            }).First();
            var parameter = new CodeParameter(method) {
                Name = "param1",
                ParameterKind = CodeParameterKind.QueryParameter,
                Type = new CodeType(method) {
                    Name = "SomeCustomType",
                    ActionOf = true,
                    TypeDefinition = new CodeClass(method) {
                        Name = "SomeCustomType"
                    }
                }
            };
            method.Parameters.Add(parameter);
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.Equal(2, model.GetChildElements(true).Count());
        }
        #endregion
        #region JavaLanguageRefinerTests
        [Fact]
        public void AddsListImport() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new CodeProperty(model){
                Name = "prop1",
                Type = new CodeType(model) {
                    Name = "string",
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
                    IsExternal = true,
                }
            });
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEmpty((model.StartBlock as CodeClass.Declaration).Usings.Where(x => "List".Equals(x.Name)));
        }
        [Fact]
        public void AddsEnumSetImport() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            model.AddProperty(new CodeProperty(model){
                Name = "prop1",
                Type = new CodeType(model) {
                    Name = "SomeEnum",
                    TypeDefinition = new CodeEnum(model) {
                        Name = "SomeEnum",
                        Flags = true,
                    }
                }
            });
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.NotEmpty((model.StartBlock as CodeClass.Declaration).Usings.Where(x => "EnumSet".Equals(x.Name)));
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
                Type = new CodeType(model) {
                    Name = httpCoreDefaultName
                }
            }, new (model) {
                Name = "serializerFactory",
                Type = new CodeType(model) {
                    Name = factoryDefaultName,
                }
            }, new (model) {
                Name = "deserializeFields",
                Type = new CodeType(model) {
                    Name = deserializeDefaultName,
                }
            }, new (model) {
                Name = "someDate",
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
            }).First();
            executorMethod.AddParameter(new (executorMethod) {
                Name = "handler",
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
                Type = new CodeType(executorMethod) {
                    Name = serializerDefaultName,
                }
            });
            ILanguageRefiner.Refine(GenerationLanguage.Java, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => httpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => factoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => deserializeDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        }
        #endregion
    }
}
