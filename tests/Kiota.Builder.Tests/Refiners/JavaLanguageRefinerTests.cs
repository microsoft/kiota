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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
            Assert.NotEqual("break", nUsing.Declaration.Name);
            Assert.Contains("escaped", nUsing.Declaration.Name);
        }
        [Fact]
        public void EscapesReservedKeywords() {
            var model = root.AddClass(new CodeClass (root) {
                Name = "break",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
            Assert.Single(requestBuilder.GetChildElements(true).OfType<CodeProperty>());
            Assert.Empty(requestBuilder.GetChildElements(true).OfType<CodeIndexer>());
            Assert.Single(collectionRequestBuilder.GetChildElements(true).OfType<CodeMethod>().Where(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)));
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
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
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => httpCoreDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => factoryDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => dateTimeOffsetDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeProperty>().Where(x => addiationalDataDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().Where(x => deserializeDefaultName.Equals(x.ReturnType.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => handlerDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => headersDefaultName.Equals(x.Type.Name)));
            Assert.Empty(model.GetChildElements(true).OfType<CodeMethod>().SelectMany(x => x.Parameters).Where(x => serializerDefaultName.Equals(x.Type.Name)));
        }
        [Fact]
        public void AddsMethodsOverloads() {
            var builder = root.AddClass(new CodeClass (root) {
                Name = "model",
                ClassKind = CodeClassKind.RequestBuilder
            }).First();
            var executor = builder.AddMethod(new CodeMethod(builder) {
                Name = "executor",
                MethodKind = CodeMethodKind.RequestExecutor,
                ReturnType = new CodeType(builder) {
                    Name = "string"
                }
            }).First();
            executor.Parameters.Add(new CodeParameter(executor) {
                Name = "handler",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = new CodeType(executor) {
                    Name = "string"
                }
            });
            executor.AddParameter(new CodeParameter(executor) {
                Name = "headers",
                ParameterKind = CodeParameterKind.Headers,
                Type = new CodeType(executor) {
                    Name = "string"
                }
            });
            executor.AddParameter(new CodeParameter(executor) {
                Name = "query",
                ParameterKind = CodeParameterKind.QueryParameter,
                Type = new CodeType(executor) {
                    Name = "string"
                }
            });
            executor.AddParameter(new CodeParameter(executor) {
                Name = "body",
                ParameterKind = CodeParameterKind.RequestBody,
                Type = new CodeType(executor) {
                    Name = "string"
                }
            });
            executor.AddParameter(new CodeParameter(executor) {
                Name = "options",
                ParameterKind = CodeParameterKind.Options,
                Type = new CodeType(executor) {
                    Name = "string"
                }
            });
            var generator = builder.AddMethod(new CodeMethod(builder) {
                Name = "generator",
                MethodKind = CodeMethodKind.RequestGenerator,
                ReturnType = new CodeType(builder) {
                    Name = "string"
                }
            }).First();
            generator.Parameters.AddRange(executor.Parameters.Where(x => !x.IsOfKind(CodeParameterKind.ResponseHandler)));
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.Java }, root);
            var childMethods = builder.GetChildElements(true).OfType<CodeMethod>();
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count == 1);//only the body
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count == 1);//only the body
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count == 2);// body + query params
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count == 2);// body + query params
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count == 3);// body + query params + headers
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count == 3);// body + query params + headers
            Assert.Contains(childMethods, x => x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count == 4);// body + query params + headers + options
            Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestGenerator) && x.Parameters.Count == 4);// body + query params + headers + options
            Assert.Contains(childMethods, x => !x.IsOverload && x.IsOfKind(CodeMethodKind.RequestExecutor) && x.Parameters.Count == 5);// body + query params + headers + options + response handler
            Assert.Equal(9, childMethods.Count());
            Assert.Equal(7, childMethods.Count(x => x.IsOverload));
        }
        #endregion
    }
}
