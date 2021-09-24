using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class CodeMethodWriterTests : IDisposable {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private readonly CodeClass parentClass;
        private const string MethodName = "methodName";
        private const string ReturnTypeName = "Somecustomtype";
        private const string MethodDescription = "some description";
        private const string ParamDescription = "some parameter description";
        private const string ParamName = "paramName";
        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Java, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
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
            parentClass.AddMethod(method);
        }
        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
        private void AddRequestProperties() {
            parentClass.AddProperty(new CodeProperty {
                Name = "httpCore",
                PropertyKind = CodePropertyKind.HttpCore,
            });
            parentClass.AddProperty(new CodeProperty {
                Name = "isRawUrl",
                PropertyKind = CodePropertyKind.RawUrl,
            });
            parentClass.AddProperty(new CodeProperty {
                Name = "currentPath",
                PropertyKind = CodePropertyKind.CurrentPath,
            });
            parentClass.AddProperty(new CodeProperty {
                Name = "pathSegment",
                PropertyKind = CodePropertyKind.PathSegment,
            });
        }
        private void AddSerializationProperties() {
            var addData = parentClass.AddProperty(new CodeProperty {
                Name = "additionalData",
                PropertyKind = CodePropertyKind.AdditionalData,
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
                    Name = "SomeComplexType"
                }
            };
            var dummyEnumProp = parentClass.AddProperty(new CodeProperty{
                Name = "dummyEnumCollection",
            }).First();
            dummyEnumProp.Type = new CodeType {
                Name = "SomeEnum",
                TypeDefinition = new CodeEnum {
                    Name = "EnumType"
                }
            };
        }
        private void AddInheritanceClass() {
            (parentClass.StartBlock as CodeClass.Declaration).Inherits = new CodeType {
                Name = "someParentClass"
            };
        }
        private void AddRequestBodyParameters() {
            var stringType = new CodeType {
                Name = "string",
            };
            method.AddParameter(new CodeParameter {
                Name = "h",
                ParameterKind = CodeParameterKind.Headers,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "q",
                ParameterKind = CodeParameterKind.QueryParameter,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "b",
                ParameterKind = CodeParameterKind.RequestBody,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter{
                Name = "r",
                ParameterKind = CodeParameterKind.ResponseHandler,
                Type = stringType,
            });
            method.AddParameter(new CodeParameter {
                Name = "o",
                ParameterKind = CodeParameterKind.Options,
                Type = stringType,
            });
        }
        [Fact]
        public void WritesNullableVoidTypeForExecutor(){
            method.MethodKind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            method.ReturnType = new CodeType {
                Name = "void",
            };
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("CompletableFuture<Void>", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestBuilder() {
            method.MethodKind = CodeMethodKind.RequestBuilderBackwardCompatibility;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesRequestBodiesThrowOnNullHttpMethod() {
            method.MethodKind = CodeMethodKind.RequestExecutor;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
            method.MethodKind = CodeMethodKind.RequestGenerator;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesRequestExecutorBody() {
            method.MethodKind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("final RequestInformation requestInfo", result);
            Assert.Contains("sendAsync", result);
            Assert.Contains("CompletableFuture.failedFuture(ex)", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestExecutorBodyForCollections() {
            method.MethodKind = CodeMethodKind.RequestExecutor;
            method.HttpMethod = HttpMethod.Get;
            method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("sendCollectionAsync", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestGeneratorBody() {
            method.MethodKind = CodeMethodKind.RequestGenerator;
            method.HttpMethod = HttpMethod.Get;
            AddRequestProperties();
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("final RequestInformation requestInfo = new RequestInformation()", result);
            Assert.Contains("this.setUri", result);
            Assert.Contains("httpMethod = HttpMethod.GET", result);
            Assert.Contains("h.accept(requestInfo.headers)", result);
            Assert.Contains("AddQueryParameters", result);
            Assert.Contains("setContentFromParsable", result);
            Assert.Contains("addMiddlewareOptions", result);
            Assert.Contains("return requestInfo;", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestGeneratorOverloadBody() {
            method.MethodKind = CodeMethodKind.RequestGenerator;
            method.HttpMethod = HttpMethod.Get;
            method.OriginalMethod = method;
            AddRequestBodyParameters();
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain("final RequestInformation requestInfo = new RequestInformation()", result);
            Assert.DoesNotContain("httpMethod = HttpMethod.GET", result);
            Assert.DoesNotContain("h.accept(requestInfo.headers)", result);
            Assert.DoesNotContain("AddQueryParameters", result);
            Assert.DoesNotContain("setContentFromParsable", result);
            Assert.DoesNotContain("addMiddlewareOptions", result);
            Assert.DoesNotContain("return requestInfo;", result);
            Assert.Contains("return methodName(b, q, h, o)", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesInheritedDeSerializerBody() {
            method.MethodKind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super.methodName()", result);
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
            method.MethodKind = CodeMethodKind.Deserializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("getStringValue", result);
            Assert.Contains("getCollectionOfPrimitiveValues", result);
            Assert.Contains("getCollectionOfObjectValues", result);
            Assert.Contains("getEnumValue", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesInheritedSerializerBody() {
            method.MethodKind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("super.serialize", result);
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
            method.MethodKind = CodeMethodKind.Serializer;
            method.IsAsync = false;
            AddSerializationProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("writeStringValue", result);
            Assert.Contains("writeCollectionOfPrimitiveValues", result);
            Assert.Contains("writeCollectionOfObjectValues", result);
            Assert.Contains("writeEnumValue", result);
            Assert.Contains("writeAdditionalData(this.getAdditionalData());", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesMethodAsyncDescription() {
            
            method.Description = MethodDescription;
            var parameter = new CodeParameter{
                Description = ParamDescription,
                Name = ParamName
            };
            parameter.Type = new CodeType {
                Name = "string"
            };
            method.AddParameter(parameter);
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("/**", result);
            Assert.Contains(MethodDescription, result);
            Assert.Contains("@param ", result);
            Assert.Contains(ParamName, result);
            Assert.Contains(ParamDescription, result); 
            Assert.Contains("@return a CompletableFuture of", result);
            Assert.Contains("*/", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesMethodSyncDescription() {
            
            method.Description = MethodDescription;
            method.IsAsync = false;
            var parameter = new CodeParameter{
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
            var codeMethodWriter = new CodeMethodWriter(new JavaConventionService());
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(null, writer));
            Assert.Throws<ArgumentNullException>(() => codeMethodWriter.WriteCodeElement(method, null));
            var originalParent = method.Parent;
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
            method.Parent = originalParent;
            method.ReturnType = null;
            Assert.Throws<InvalidOperationException>(() => codeMethodWriter.WriteCodeElement(method, writer));
        }
        [Fact]
        public void ThrowsIfParentIsNotClass() {
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void ThrowsIfReturnTypeIsMissing() {
            method.ReturnType = null;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        private const string TaskPrefix = "CompletableFuture<";
        [Fact]
        public void WritesReturnType() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains($"{TaskPrefix}{ReturnTypeName}> {MethodName}", result);// async default
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void DoesNotAddAsyncInformationOnSyncMethods() {
            method.IsAsync = false;
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain(TaskPrefix, result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesPublicMethodByDefault() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("public ", result);// public default
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesPrivateMethod() {
            method.Access = AccessModifier.Private;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("private ", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesProtectedMethod() {
            method.Access = AccessModifier.Protected;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("protected ", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesIndexer() {
            method.MethodKind = CodeMethodKind.IndexerBackwardCompatibility;
            method.PathSegment = "somePath";
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("httpCore", result);
            Assert.Contains("pathSegment", result);
            Assert.Contains("+ id", result);
            Assert.Contains("return new", result);
            Assert.Contains(method.PathSegment, result);
        }
        [Fact]
        public void WritesGetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.getBackingStore().get(\"someProperty\")", result);
        }
        [Fact]
        public void WritesGetterToBackingStoreWithNonnullProperty() {
            method.AddAccessedProperty();
            parentClass.AddBackingStoreProperty();
            method.AccessedProperty.Type = new CodeType {
                Name = "string",
                IsNullable = false,
            };
            var defaultValue = "someDefaultValue";
            method.AccessedProperty.DefaultValue = defaultValue; 
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("if(value == null)", result);
            Assert.Contains(defaultValue, result);
        }
        [Fact]
        public void WritesSetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.getBackingStore().set(\"someProperty\", value)", result);
        }
        [Fact]
        public void WritesGetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.someProperty", result);
        }
        [Fact]
        public void WritesSetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("this.someProperty = value", result);
        }
        [Fact]
        public void WritesConstructor() {
            method.MethodKind = CodeMethodKind.Constructor;
            var defaultValue = "someVal";
            var propName = "propWithDefaultValue";
            parentClass.AddProperty(new CodeProperty {
                Name = propName,
                DefaultValue = defaultValue,
                PropertyKind = CodePropertyKind.PathSegment,
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains($"this.{propName} = {defaultValue}", result);
        }
        [Fact]
        public void WritesApiConstructor() {
            method.MethodKind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
            }).First();
            coreProp.Type = new CodeType {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter {
                Name = "core",
                ParameterKind = CodeParameterKind.HttpCore,
                Type = coreProp.Type,
            });
            method.DeserializerModules = new() {"com.microsoft.kiota.serialization.Deserializer"};
            method.SerializerModules = new() {"com.microsoft.kiota.serialization.Serializer"};
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains("registerDefaultSerializer", result);
            Assert.Contains("registerDefaultDeserializer", result);
        }
        [Fact]
        public void WritesApiConstructorWithBackingStore() {
            method.MethodKind = CodeMethodKind.ClientConstructor;
            var coreProp = parentClass.AddProperty(new CodeProperty {
                Name = "core",
                PropertyKind = CodePropertyKind.HttpCore,
            }).First();
            coreProp.Type = new CodeType {
                Name = "HttpCore",
                IsExternal = true,
            };
            method.AddParameter(new CodeParameter {
                Name = "core",
                ParameterKind = CodeParameterKind.HttpCore,
                Type = coreProp.Type,
            });
            var backingStoreParam = new CodeParameter {
                Name = "backingStore",
                ParameterKind = CodeParameterKind.BackingStore,
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
