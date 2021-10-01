using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Tests;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
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
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
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
            Assert.Contains("(func() (error))", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesRequestBuilder() {
            method.MethodKind = CodeMethodKind.RequestBuilderBackwardCompatibility;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.pathSegment", result);
            Assert.Contains("m.httpCore", result);
            Assert.Contains("return", result);
            Assert.Contains("func (m", result);
            Assert.Contains("New", result);
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
            Assert.Contains("requestInfo, err :=", result);
            Assert.Contains("m.httpCore.SendAsync", result);
            Assert.Contains("return res.(", result);
            Assert.Contains("err != nil", result);
            Assert.Contains("return func() (", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        private const string AbstractionsPackageHash = "ida96af0f171bb75f894a4013a6b3146a4397c58f11adb81a2b7cbea9314783a9";
        [Fact]
        public void WritesRequestGeneratorBody() {
            method.MethodKind = CodeMethodKind.RequestGenerator;
            method.HttpMethod = HttpMethod.Get;
            AddRequestBodyParameters();
            AddRequestProperties();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains($"requestInfo := {AbstractionsPackageHash}.NewRequestInformation()", result);
            Assert.Contains("err := requestInfo.SetUri", result);
            Assert.Contains($"Method = {AbstractionsPackageHash}.GET", result);
            Assert.Contains("err != nil", result);
            Assert.Contains("h != nil", result);
            Assert.Contains("h(requestInfo.Headers)", result);
            Assert.Contains("q != nil", result);
            Assert.Contains("qParams.AddQueryParameters(requestInfo.QueryParameters)", result);
            Assert.Contains("o != nil", result);
            Assert.Contains("requestInfo.AddRequestOptions(o)", result);
            Assert.Contains("requestInfo.SetContentFromParsable(m.httpCore", result);
            Assert.Contains("return requestInfo, err", result);
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
            Assert.Contains("m.SomeParentClass.MethodName()", result);
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
            Assert.Contains("GetStringValue", result);
            Assert.Contains("GetCollectionOfPrimitiveValues", result);
            Assert.Contains("GetCollectionOfObjectValues", result);
            Assert.Contains("GetEnumValue", result);
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
            Assert.Contains("m.SomeParentClass.Serialize", result);
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
            Assert.Contains("WritePrimitiveValue", result);
            Assert.Contains("WriteCollectionOfPrimitiveValues", result);
            Assert.Contains("WriteCollectionOfObjectValues", result);
            // Assert.Contains("WriteEnumValue", result); update when implementing enum serialization
            Assert.Contains("WriteAdditionalData(m.GetAdditionalData())", result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact(Skip = "descriptions are not supported")]
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
            var codeMethodWriter = new CodeMethodWriter(new GoConventionService());
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
        private const string TaskPrefix = "func() (";
        [Fact]
        public void WritesReturnType() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains($"{MethodName.ToFirstCharacterUpperCase()}()({TaskPrefix}*{ReturnTypeName}, error)", result);// async default
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
            Assert.Contains(MethodName.ToFirstCharacterUpperCase(), result);// public default
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesPrivateMethod() {
            method.Access = AccessModifier.Private;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesProtectedMethod() {
            method.Access = AccessModifier.Protected;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(MethodName.ToFirstCharacterLowerCase(), result);
            AssertExtensions.CurlyBracesAreClosed(result);
        }
        [Fact]
        public void WritesIndexer() {
            method.MethodKind = CodeMethodKind.IndexerBackwardCompatibility;
            method.PathSegment = "somePath";
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.httpCore", result);
            Assert.Contains("m.pathSegment", result);
            Assert.Contains("+ id", result);
            Assert.Contains("return", result);
            Assert.Contains("New", result);
            Assert.Contains(method.PathSegment, result);
        }
        [Fact]
        public void WritesPathParameterRequestBuilder() {
            method.MethodKind = CodeMethodKind.RequestBuilderWithParameters;
            method.PathSegment = "somePath";
            method.AddParameter(new CodeParameter {
                Name = "pathParam",
                ParameterKind = CodeParameterKind.Path,
                Type = new CodeType {
                    Name = "string"
                }
            });
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.httpCore", result);
            Assert.Contains("m.pathSegment", result);
            Assert.Contains("pathParam", result);
            Assert.Contains("return *New", result);
        }
        [Fact]
        public void WritesGetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.GetBackingStore().Get(\"someProperty\")", result);
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
            Assert.Contains("if value == nil", result);
            Assert.Contains(defaultValue, result);
        }
        [Fact]
        public void WritesSetterToBackingStore() {
            parentClass.AddBackingStoreProperty();
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.GetBackingStore().Set(\"someProperty\", value)", result);
        }
        [Fact]
        public void WritesGetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Getter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.someProperty", result);
        }
        [Fact]
        public void WritesSetterToField() {
            method.AddAccessedProperty();
            method.MethodKind = CodeMethodKind.Setter;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("m.someProperty = value", result);
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
            Assert.Contains($"m.{propName} = {defaultValue}", result);
        }
        [Fact]
        public void WritesInheritedConstructor() {
            method.MethodKind = CodeMethodKind.Constructor;
            AddInheritanceClass();
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains("SomeParentClass: *NewSomeParentClass", result);
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
            method.DeserializerModules = new() {"github.com/microsoft/kiota/serialization/go/json.Deserializer"};
            method.SerializerModules = new() {"github.com/microsoft/kiota/serialization/go/json.Serializer"};
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(parentClass.Name.ToFirstCharacterUpperCase(), result);
            Assert.Contains("RegisterDefaultSerializer", result);
            Assert.Contains("RegisterDefaultDeserializer", result);
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
                Name = "IBackingStore",
                IsExternal = true,
            };
            method.AddParameter(backingStoreParam);
            var tempWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.Go, DefaultPath, DefaultName);
            tempWriter.SetTextWriter(tw);
            tempWriter.Write(method);
            var result = tw.ToString();
            Assert.Contains("EnableBackingStore", result);
        }
    }
}
