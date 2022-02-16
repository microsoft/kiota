using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeMethodWriterTests : IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private readonly CodeMethod voidMethod;
        private readonly CodeClass parentClass;
        private const string MethodName = "methodName";
        private const string ReturnTypeName = "Somecustomtype";
        private const string MethodDescription = "some description";
        private const string ParamDescription = "some parameter description";
        private const string ParamName = "paramName";
        private readonly CodeNamespace root;
        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod {
                Name = MethodName,
            };
            method.ReturnType = new CodeType {
                Name = ReturnTypeName
            };
            voidMethod = new CodeMethod {
                Name = MethodName,
            };
            voidMethod.ReturnType = new CodeType {
                Name = "void"
            };
            parentClass.AddMethod(voidMethod);
            parentClass.AddMethod(method);
        }
        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        private void AddRequestProperties() {
            parentClass.AddProperty(new CodeProperty {
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
            });
            parentClass.AddProperty(new CodeProperty {
                Name = "pathParameters",
                Kind = CodePropertyKind.PathParameters,
                Type = new CodeType {
                    Name = "string",
                }
            });
            parentClass.AddProperty(new CodeProperty {
                Name = "urlTemplate",
                Kind = CodePropertyKind.UrlTemplate,
            });
        }
        private void AddSerializationProperties() {
            var addData = parentClass.AddProperty(new CodeProperty {
                Name = "additionalData",
                Kind = CodePropertyKind.AdditionalData,
            }).First();
            addData.Type = new CodeType {
                Name = "string"
            };
            var dummyProp = parentClass.AddProperty(new CodeProperty {
                Name = "dummyProp",
            }).First();
            dummyProp.Type = new CodeType {
                Name = "string"
            };
            var dummyCollectionProp = parentClass.AddProperty(new CodeProperty {
                Name = "dummyColl",
            }).First();
            dummyCollectionProp.Type = new CodeType {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyComplexCollection = parentClass.AddProperty(new CodeProperty {
                Name = "dummyComplexColl"
            }).First();
            dummyComplexCollection.Type = new CodeType {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = new CodeClass {
                    Name = "SomeComplexType",
                    Parent = root.AddNamespace("models")
                }
            };
            var dummyEnumProp = parentClass.AddProperty(new CodeProperty {
                Name = "dummyEnumCollection",
            }).First();
            dummyEnumProp.Type = new CodeType {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum {
                    Name = "EnumType",
                    Parent = root.AddNamespace("models")
                }
            };
        }
        private void AddInheritanceClass() {
            (parentClass.StartBlock as CodeClass.ClassDeclaration).Inherits = new CodeType {
                Name = "someParentClass"
            };
        }
        private void AddRequestBodyParameters() {
            var stringType = new CodeType {
                Name = "string",
            };
            method.AddParameter(new CodeParameter {
                Name = "h",
                Kind = CodeParameterKind.Headers,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "q",
                Kind = CodeParameterKind.QueryParameter,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "b",
                Kind = CodeParameterKind.RequestBody,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "r",
                Kind = CodeParameterKind.ResponseHandler,
                Type = stringType,
            });
        }
        private void AddVoidRequestBodyParameters() {
            var stringType = new CodeType {
                Name = "string",
            };
            voidMethod.AddParameter(new CodeParameter {
                Name = "h",
                Kind = CodeParameterKind.Headers,
                Type = stringType,
            });
            voidMethod.AddParameter(new CodeParameter{
                Name = "q",
                Kind = CodeParameterKind.QueryParameter,
                Type = stringType,
            });
            voidMethod.AddParameter(new CodeParameter{
                Name = "b",
                Kind = CodeParameterKind.RequestBody,
                Type = stringType,
            });
            voidMethod.AddParameter(new CodeParameter{
                Name = "r",
                Kind = CodeParameterKind.ResponseHandler,
                Type = stringType,
            });
        }
        [Fact]
        public void WritesRequestBuilder() {
            method.Kind = CodeMethodKind.RequestBuilderBackwardCompatibility;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesRequestBodiesThrowOnNullHttpMethod() {
            method.Kind = CodeMethodKind.RequestExecutor;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
            method.Kind = CodeMethodKind.RequestGenerator;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesRequestExecutorBody() {
            method.Kind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("request_info", result);
            Assert.Contains("send_async", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestExecutorBodyWithNamespace() {
            voidMethod.Kind = CodeMethodKind.RequestExecutor;
            voidMethod.HttpMethod = HttpMethod.Get;
            AddVoidRequestBodyParameters();
            writer.Write(voidMethod);
            var result = tw.ToString();
            Assert.Contains("request_info", result);
            Assert.Contains("send_async", result);
            Assert.Contains("nil", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestGeneratorBody() {
            method.Kind = CodeMethodKind.RequestGenerator;
            method.HttpMethod = HttpMethod.Get;
            AddRequestProperties();
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("request_info = MicrosoftKiotaAbstractions::RequestInformation.new()", result);
            Assert.Contains("request_info.path_parameters", result);
            Assert.Contains("request_info.url_template", result);
            Assert.Contains("http_method = :GET", result);
            Assert.Contains("set_query_string_parameters_from_raw_object", result);
            Assert.Contains("set_content_from_parsable", result);
            Assert.Contains("return request_info;", result);
        }
        [Fact]
        public void WritesInheritedDeSerializerBody() {
            method.Kind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super.merge({", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesDeSerializerBody() {
            var parameter = new CodeParameter{
                Description = ParamDescription,
                Name = ParamName
            };
            parameter.Type = new CodeType {
                Name = "string"
            };
            method.Kind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("get_collection_of_primitive_values", result);
            Assert.Contains("get_collection_of_object_values", result);
            Assert.Contains("get_enum_value", result);
        }
        [Fact]
        public void WritesInheritedSerializerBody() {
            method.Kind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesSerializerBody() {
            var parameter = new CodeParameter{
                Description = ParamDescription,
                Name = ParamName
            };
            parameter.Type = new CodeType {
                Name = "string"
            };
            method.Kind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("write_collection_of_primitive_values", result);
            Assert.Contains("write_collection_of_object_values", result);
            Assert.Contains("write_enum_value", result);
            Assert.Contains("write_additional_data", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesTranslatedTypesDeSerializerBody() {
            var dummyCollectionProp1 = parentClass.AddProperty(new CodeProperty {
                Name = "guidId",
            }).First();
            dummyCollectionProp1.Type = new CodeType {
                Name = "guid",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyCollectionProp2 = parentClass.AddProperty(new CodeProperty {
                Name = "dateTime",
            }).First();
            dummyCollectionProp2.Type = new CodeType {
                Name = "date",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyCollectionProp3 = parentClass.AddProperty(new CodeProperty {
                Name = "isTrue",
            }).First();
            dummyCollectionProp3.Type = new CodeType {
                Name = "boolean",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyCollectionProp4 = parentClass.AddProperty(new CodeProperty {
                Name = "numberTest",
            }).First();
            dummyCollectionProp4.Type = new CodeType {
                Name = "number",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyCollectionProp5 = parentClass.AddProperty(new CodeProperty {
                Name = "DatetimeValueType",
            }).First();
            dummyCollectionProp5.Type = new CodeType {
                Name = "dateTimeOffset",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            var dummyCollectionProp6 = parentClass.AddProperty(new CodeProperty {
                Name = "messages",
            }).First();
            dummyCollectionProp6.Type = new CodeType {
                Name = "NewObjectName",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            };
            method.Kind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("get_collection_of_primitive_values(String)", result);
            Assert.Contains("get_collection_of_primitive_values(\"boolean\")", result);
            Assert.Contains("get_collection_of_primitive_values(Integer)", result);
            Assert.Contains("get_collection_of_primitive_values(Time)", result);
            Assert.Contains("get_collection_of_primitive_values(UUIDTools::UUID)", result);
            Assert.Contains("get_collection_of_primitive_values(NewObjectName)", result);
        }
        [Fact]
        public void WritesMethodSyncDescription() {
            
            method.Description = MethodDescription;
            method.IsAsync = false;
            var parameter = new CodeParameter {
                Description = ParamDescription,
                Name = ParamName
            };
            parameter.Type = new CodeType {
                Name = "string"
            };
            method.AddParameter(parameter);
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain("@return a CompletableFuture of", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void Defensive() {
            var codeMethodWriter = new CodeMethodWriter(new RubyConventionService());
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
            var originalParent = method.Parent;
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        }
        [Fact]
        public void ThrowsIfParentIsNotClass() {
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        private const string TaskPrefix = "CompletableFuture<";
        [Fact]
        public void DoesNotAddAsyncInformationOnSyncMethods() {
            method.IsAsync = false;
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain(TaskPrefix, result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesGetterToField() {
            method.AddAccessedProperty();
            method.Kind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("@some_property", result);
        }
        [Fact]
        public void WritesIndexer() {
            AddRequestProperties();
            method.Kind = CodeMethodKind.IndexerBackwardCompatibility;
            method.OriginalIndexer = new () {
                Name = "indx",
                ParameterName = "id",
                IndexType = new CodeType {
                    Name = "string",
                    IsNullable = true,
                }
            };
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("request_adapter", result);
            Assert.Contains("path_parameters", result);
            Assert.Contains("= id", result);
            Assert.Contains("return Somecustomtype.new", result);
        }
        [Fact]
        public void WritesPathParameterRequestBuilder() {
            AddRequestProperties();
            method.Kind = CodeMethodKind.RequestBuilderWithParameters;
            method.AddParameter(new CodeParameter {
                Name = "pathParam",
                Kind = CodeParameterKind.Path,
                Type = new CodeType {
                    Name = "string"
                }
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("request_adapter", result);
            Assert.Contains("path_parameters", result);
            Assert.Contains("pathParam", result);
            Assert.Contains("return Somecustomtype.new", result);
        }
        [Fact]
        public void WritesSetterToField() {
            method.AddAccessedProperty();
            method.Kind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("@some_property =", result);
        }
        [Fact]
        public void WritesConstructor() {
            method.Kind = CodeMethodKind.Constructor;
            var defaultValue = "someval";
            var propName = "propWithDefaultValue";
            parentClass.AddProperty(new CodeProperty {
                Name = propName,
                DefaultValue = defaultValue,
                Kind = CodePropertyKind.UrlTemplate,
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains($"@{propName.ToSnakeCase()} = {defaultValue}", result);
        }
        [Fact]
        public void WritesApiConstructor() {
            method.Kind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty {
                Name = "core",
                Kind = CodePropertyKind.RequestAdapter,
            }).First();
            coreProp.Type = new CodeType {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter {
                Name = "core",
                Kind = CodeParameterKind.RequestAdapter,
                Type = coreProp.Type,
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(coreProp.Name, result);
        }
        [Fact]
        public void WritesApiConstructorWithBackingStore() {
            method.Kind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty {
                Name = "core",
                Kind = CodePropertyKind.RequestAdapter,
            }).First();
            coreProp.Type = new CodeType {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter {
                Name = "core",
                Kind = CodeParameterKind.RequestAdapter,
                Type = coreProp.Type,
            });
            var backingStoreParam = new CodeParameter {
                Name = "backingStore",
                Kind = CodeParameterKind.BackingStore,
            };
            backingStoreParam.Type = new CodeType {
                Name = "BackingStore",
                IsExternal = true,
            };
            method.AddParameter(backingStoreParam);
            var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
            tempWriter.SetTextWriter(tw);
            tempWriter.Write(method);
            var result = tw.ToString();
            Assert.Contains("enableBackingStore", result);
        }
    }
}
