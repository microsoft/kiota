using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeMethodWriterTests: IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter stringWriter;
        private readonly LanguageWriter languageWriter;
        private readonly CodeMethod method;
        private readonly CodeClass parentClass;
        private const string MethodName = "methodName";
        private const string ReturnTypeName = "Promise";
        private const string MethodDescription = "some description";
        private const string ParamDescription = "some parameter description";
        private const string ParamName = "paramName";
        private CodeMethodWriter _codeMethodWriter;
        private readonly ILanguageRefiner _refiner;
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();

        public CodeMethodWriterTests()
        {
            languageWriter = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            stringWriter = new StringWriter();
            languageWriter.SetTextWriter(stringWriter);
            root = CodeNamespace.InitRootNamespace();
            root.Name = "Microsoft\\Graph";
            _codeMethodWriter = new CodeMethodWriter(new PhpConventionService());
            parentClass = new CodeClass
            {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod
            {
                Name = MethodName,
                IsAsync = true,
                Documentation = new() {
                    Description = "This is a very good method to try all the good things",
                },
                ReturnType = new CodeType
                {
                    Name = ReturnTypeName
                }
            };
            _refiner = new PhpRefiner(new GenerationConfiguration {Language = GenerationLanguage.PHP});
            parentClass.AddMethod(method);
        }
        
        
        private void AddRequestProperties()
        {
            parentClass.AddProperty(
                new CodeProperty
                {
                    Name = "urlTemplate",
                    Access = AccessModifier.Protected,
                    DefaultValue = "https://graph.microsoft.com/v1.0/",
                    Documentation = new() {
                        Description = "The URL template",
                    },
                    Kind = CodePropertyKind.UrlTemplate,
                    Type = new CodeType {Name = "string"}
                },
                new CodeProperty
                {
                    Name = "pathParameters",
                    Access = AccessModifier.Protected,
                    DefaultValue = "[]",
                    Documentation = new() {
                        Description = "The Path parameters.",
                    },
                    Kind = CodePropertyKind.PathParameters,
                    Type = new CodeType {Name = "array"}
                },
                new CodeProperty
                {
                    Name = "requestAdapter",
                    Access = AccessModifier.Protected,
                    Documentation = new() {
                        Description = "The request Adapter",
                    },
                    Kind = CodePropertyKind.RequestAdapter,
                    Type = new CodeType
                    {
                        IsNullable = false,
                        Name = "RequestAdapter"
                    }
                }
            );
        }

        private void AddRequestBodyParameters()
        {
            var stringType = new CodeType {
                Name = "string",
                IsNullable = false
            };
            var requestConfigClass = parentClass.AddInnerClass(new CodeClass {
                Name = "RequestConfig",
                Kind = CodeClassKind.RequestConfiguration,
            }).First();
            
            requestConfigClass.AddProperty(new() {
                    Name = "h",
                    Kind = CodePropertyKind.Headers,
                    Type = stringType,
                },
                new () {
                    Name = "q",
                    Kind = CodePropertyKind.QueryParameters,
                    Type = stringType,
                },
                new () {
                    Name = "o",
                    Kind = CodePropertyKind.Options,
                    Type = stringType,
                }
            );
            
            method.AddParameter(
                new CodeParameter
                {
                    Name = "body",
                    Kind = CodeParameterKind.RequestBody,
                    Type = new CodeType
                    {
                        Name = "Message",
                        IsExternal = true,
                        IsNullable = false,
                        TypeDefinition = root.AddClass(new CodeClass {
                            Name = "SomeComplexTypeForRequestBody",
                            Kind = CodeClassKind.Model,
                        }).First()
                    },
                },
                new CodeParameter{
                    Name = "config",
                    Kind = CodeParameterKind.RequestConfiguration,
                    Type = new CodeType {
                        Name = "RequestConfig",
                        TypeDefinition = requestConfigClass,
                        ActionOf = true,
                    },
                    Optional = true,
                }
            );
        }
        
        [Fact]
        public void WriteABasicMethod()
        {
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("public function", result);
        }

        [Fact]
        public void WriteMethodWithNoDescription()
        {
            method.Documentation.Description = null;
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            
            Assert.DoesNotContain("/*", result);
        }

        public void Dispose()
        {
            stringWriter?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void WriteRequestExecutor()
        {
            CodeProperty[] properties =
            {
                new CodeProperty { Kind = CodePropertyKind.RequestAdapter, Name = "requestAdapter" },
                new CodeProperty { Kind = CodePropertyKind.UrlTemplate, Name = "urlTemplate" },
                new CodeProperty { Kind = CodePropertyKind.PathParameters, Name = "pathParameters" },
            };
            parentClass.AddProperty(properties);
            var codeMethod = new CodeMethod
            {
                Name = "post",
                HttpMethod = HttpMethod.Post,
                ReturnType = new CodeType
                {
                    IsExternal = true,
                    Name = "StreamInterface"
                },
                Documentation = new() {
                    Description = "This will send a POST request",
                    DocumentationLink = new Uri("https://learn.microsoft.com/"),
                    DocumentationLabel = "Learning"
                },
                Kind = CodeMethodKind.RequestExecutor
            };
            var codeMethodRequestGenerator = new CodeMethod
            {
                Kind = CodeMethodKind.RequestGenerator,
                HttpMethod = HttpMethod.Post,
                Name = "createPostRequestInformation",
                ReturnType = new CodeType
                {
                    Name = "RequestInformation"
                }
            };
            parentClass.AddMethod(codeMethod);
            parentClass.AddMethod(codeMethodRequestGenerator);
            var error4XX = root.AddClass(new CodeClass{
                Name = "Error4XX",
            }).First();
            var error5XX = root.AddClass(new CodeClass{
                Name = "Error5XX",
            }).First();
            var error401 = root.AddClass(new CodeClass{
                Name = "Error401",
            }).First();
            codeMethod.AddErrorMapping("4XX", new CodeType {Name = "Error4XX", TypeDefinition = error4XX});
            codeMethod.AddErrorMapping("5XX", new CodeType {Name = "Error5XX", TypeDefinition = error5XX});
            codeMethod.AddErrorMapping("403", new CodeType {Name = "Error403", TypeDefinition = error401});
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("Promise", result);
            Assert.Contains("$requestInfo = $this->createPostRequestInformation();", result);
            Assert.Contains("RejectedPromise", result);
            Assert.Contains("@link https://learn.microsoft.com/ Learning", result);
            Assert.Contains("catch(Exception $ex)", result);
            Assert.Contains("'403' => [Error403::class, 'createFromDiscriminatorValue']", result);
            Assert.Contains("return $this->requestAdapter->sendPrimitiveAsync($requestInfo, StreamInterface::class, $errorMappings);", result);
        }
        
        public static IEnumerable<object[]> SerializerProperties => new List<object[]>
        {
            new object[]
            {
                new CodeProperty { Name = "name", Type = new CodeType { Name = "string" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeStringValue('name', $this->getName());"
            },
            new object[]
            {
                new CodeProperty { Name = "email", Type = new CodeType
                {
                    Name = "EmailAddress", TypeDefinition = new CodeClass { Name = "EmailAddress", Kind = CodeClassKind.Model}
                }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeObjectValue('email', $this->getEmail());"
            },
            new object[]
            {
                new CodeProperty { Name = "status", Type = new CodeType { Name = "Status", TypeDefinition = new CodeEnum
                {
                    Name = "Status", 
                    Documentation = new() {
                        Description = "Status Enum",
                    },
                }}, Access = AccessModifier.Private },
                "$writer->writeEnumValue('status', $this->getStatus());"
            },
            new object[]
            {
                new CodeProperty { Name = "architectures", Type = new CodeType
                {
                    Name = "Architecture", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array, TypeDefinition = new CodeEnum { Name = "Architecture", 
                    Documentation = new() {
                        Description = "Arch Enum, accepts x64, x86, hybrid"
                    },
                    }
                }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeCollectionOfEnumValues('architectures', $this->getArchitectures());"
            },
            new object[] { new CodeProperty { Name = "emails", Type = new CodeType
            {
                Name = "Email", TypeDefinition = new CodeClass { Name = "Email", Kind = CodeClassKind.Model}, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "$writer->writeCollectionOfObjectValues('emails', $this->getEmails());"
            },
            new object[] { new CodeProperty { Name = "temperatures", Type = new CodeType { Name = "int", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "$writer->writeCollectionOfPrimitiveValues('temperatures', $this->getTemperatures());"
            },
            // Primitive int tests
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "integer" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->getAge());" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "int32" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->getAge());" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "int64" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->getAge());" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "sbyte" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->getAge());" },
            // Float tests
            new object[] { new CodeProperty { Name = "height", Type = new CodeType { Name = "float" }, Access = AccessModifier.Private}, "$writer->writeFloatValue('height', $this->getHeight());" },
            new object[] { new CodeProperty { Name = "height", Type = new CodeType { Name = "double" }, Access = AccessModifier.Private}, "$writer->writeFloatValue('height', $this->getHeight());" },
            // Bool tests
            new object[] { new CodeProperty { Name = "married", Type = new CodeType { Name = "boolean" }, Access = AccessModifier.Private}, "$writer->writeBooleanValue('married', $this->getMarried());" },
            new object[] { new CodeProperty { Name = "slept", Type = new CodeType { Name = "bool" }, Access = AccessModifier.Private}, "$writer->writeBooleanValue('slept', $this->getSlept());" },
            // Decimal and byte tests
            new object[] { new CodeProperty { Name = "money", Type = new CodeType { Name = "decimal" }, Access = AccessModifier.Private}, "$writer->writeStringValue('money', $this->getMoney());" },
            new object[] { new CodeProperty { Name = "money", Type = new CodeType { Name = "byte" }, Access = AccessModifier.Private}, "$writer->writeStringValue('money', $this->getMoney());" },
            new object[] { new CodeProperty { Name = "dateValue", Type = new CodeType { Name = "DateTime" }, Access = AccessModifier.Private}, "$writer->writeDateTimeValue('dateValue', $this->getDateValue());" },
            new object[] { new CodeProperty { Name = "duration", Type = new CodeType { Name = "duration" }, Access = AccessModifier.Private}, "$writer->writeDateIntervalValue('duration', $this->getDuration());" },
            new object[] { new CodeProperty { Name = "stream", Type = new CodeType { Name = "binary" }, Access = AccessModifier.Private}, "$writer->writeBinaryContent('stream', $this->getStream());" },
            new object[] { new CodeProperty { Name = "definedInParent", Type = new CodeType { Name = "string"}, OriginalPropertyFromBaseType = new CodeProperty() }, "$write->writeStringValue('definedInParent', $this->getDefinedInParent());"}
        };
        
        [Theory]
        [MemberData(nameof(SerializerProperties))]
        public async void WriteSerializer(CodeProperty property, string expected)
        {
            var codeMethod = new CodeMethod
            {
                Name = "serialize",
                Kind = CodeMethodKind.Serializer,
                ReturnType = new CodeType
                {
                    Name = "void",
                }
            };
            codeMethod.AddParameter(new CodeParameter
            {
                Name = "writer",
                Kind = CodeParameterKind.Serializer,
                Type = new CodeType
                {
                    Name = "SerializationWriter"
                }
            });
            parentClass.AddMethod(codeMethod);
            parentClass.AddProperty(property);
            var propertyType = property.Type.AllTypes.FirstOrDefault()?.TypeDefinition;
            switch (propertyType)
            {
                case CodeClass:
                    root.AddClass(propertyType as CodeClass);
                    break;
                case CodeEnum:
                    root.AddEnum(propertyType as CodeEnum);
                    break;
            }
            parentClass.Kind = CodeClassKind.Model;
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, root);
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("public function serialize(SerializationWriter $writer)", result);
            if (property.ExistsInBaseType)
                Assert.DoesNotContain(expected, result);
            else
                Assert.Contains(expected, stringWriter.ToString());
        }

        [Fact]
        public void WriteRequestGeneratorForParsable()
        {
            parentClass.Kind = CodeClassKind.RequestBuilder;
            method.Name = "createPostRequestInformation";
            method.Kind = CodeMethodKind.RequestGenerator;
            method.ReturnType = new CodeType()
            {
                Name = "RequestInformation", IsNullable = false
            };
            method.HttpMethod = HttpMethod.Post;
            AddRequestProperties();
            AddRequestBodyParameters();
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains(
                "public function createPostRequestInformation(Message $body, ?RequestConfig $requestConfiguration = null): RequestInformation",
                result);
            Assert.Contains("if ($requestConfiguration !== null", result);
            Assert.Contains("if ($requestConfiguration->h !== null)", result);
            Assert.Contains("$requestInfo->addHeaders($requestConfiguration->h);", result);
            Assert.Contains("$requestInfo->setQueryParameters($requestConfiguration->q);", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$requestConfiguration->o);", result);
            Assert.Contains("$requestInfo->setContentFromParsable($this->requestAdapter, \"\", $body);", result);
            Assert.Contains("return $requestInfo;", result);
        }

        [Fact]
        public void WritesRequestGeneratorBodyForParsableCollection()
        {
            parentClass.Kind = CodeClassKind.RequestBuilder;
            method.Name = "createPostRequestInformation";
            method.Kind = CodeMethodKind.RequestGenerator;
            method.AcceptedResponseTypes.Add("application/json");
            method.ReturnType = new CodeType() { Name = "RequestInformation", IsNullable = false };
            method.HttpMethod = HttpMethod.Post;
            AddRequestProperties();
            AddRequestBodyParameters();
            var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
            bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains(
                "public function createPostRequestInformation(array $body, ?RequestConfig $requestConfiguration = null): RequestInformation",
                result);
            Assert.Contains("if ($requestConfiguration !== null", result);
            Assert.Contains("if ($requestConfiguration->h !== null)", result);
            Assert.Contains("$requestInfo->addHeaders($requestConfiguration->h);", result);
            Assert.Contains("$requestInfo->setQueryParameters($requestConfiguration->q);", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$requestConfiguration->o);", result);
            Assert.Contains("$requestInfo->setContentFromParsableCollection($this->requestAdapter, \"\", $body);", result);
            Assert.Contains("return $requestInfo;", result);
        }
        
        [Fact]
        public void WriteRequestGeneratorForScalarType()
        {
            parentClass.Kind = CodeClassKind.RequestBuilder;
            method.Name = "createPostRequestInformation";
            method.Kind = CodeMethodKind.RequestGenerator;
            method.ReturnType = new CodeType() { Name = "RequestInformation", IsNullable = false };
            method.HttpMethod = HttpMethod.Post;
            AddRequestProperties();
            AddRequestBodyParameters();
            var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
            bodyParameter.Type = new CodeType() { Name = "string", IsNullable = false };
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains(
                "public function createPostRequestInformation(string $body, ?RequestConfig $requestConfiguration = null): RequestInformation",
                result);
            Assert.Contains("if ($requestConfiguration !== null", result);
            Assert.Contains("if ($requestConfiguration->h !== null)", result);
            Assert.Contains("$requestInfo->addHeaders($requestConfiguration->h);", result);
            Assert.Contains("$requestInfo->setQueryParameters($requestConfiguration->q);", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$requestConfiguration->o);", result);
            Assert.Contains("$requestInfo->setContentFromScalar($this->requestAdapter, \"\", $body);", result);
            Assert.Contains("return $requestInfo;", result);
        }
        
        [Fact]
        public void WritesRequestGeneratorBodyForScalarCollection()
        {
            parentClass.Kind = CodeClassKind.RequestBuilder;
            method.Name = "createPostRequestInformation";
            method.Kind = CodeMethodKind.RequestGenerator;
            method.AcceptedResponseTypes.Add("application/json");
            method.ReturnType = new CodeType()
            {
                Name = "RequestInformation", IsNullable = false
            };
            method.HttpMethod = HttpMethod.Post;
            AddRequestProperties();
            AddRequestBodyParameters();
            var bodyParameter = method.Parameters.OfKind(CodeParameterKind.RequestBody);
            bodyParameter.Type = new CodeType() { Name = "string", IsNullable = false };
            bodyParameter.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains(
                "public function createPostRequestInformation(array $body, ?RequestConfig $requestConfiguration = null): RequestInformation",
                result);
            Assert.Contains("if ($requestConfiguration !== null", result);
            Assert.Contains("if ($requestConfiguration->h !== null)", result);
            Assert.Contains("$requestInfo->addHeaders($requestConfiguration->h);", result);
            Assert.Contains("$requestInfo->setQueryParameters($requestConfiguration->q);", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$requestConfiguration->o);", result);
            Assert.Contains("$requestInfo->setContentFromScalarCollection($this->requestAdapter, \"\", $body);", result);
            Assert.Contains("return $requestInfo;", result);
        }

        [Fact]
        public async Task WriteIndexerBody()
        {
            parentClass.AddProperty(
                new CodeProperty
                {
                    Name = "pathParameters",
                    Kind = CodePropertyKind.PathParameters,
                    Type = new CodeType {Name = "array"},
                    DefaultValue = "[]"
                },
                new CodeProperty
                {
                    Name = "requestAdapter",
                    Kind = CodePropertyKind.RequestAdapter,
                    Type = new CodeType
                    {
                        Name = "requestAdapter"
                    }
                },
                new CodeProperty
                {
                    Name = "urlTemplate",
                    Kind = CodePropertyKind.UrlTemplate,
                    Type = new CodeType
                    {
                        Name = "string"
                    }
                }
            );
            var codeMethod = new CodeMethod
            {
                Name = "messageById",
                Access = AccessModifier.Public,
                Kind = CodeMethodKind.IndexerBackwardCompatibility,
                Documentation = new() {
                    Description = "Get messages by a specific ID.",
                },
                OriginalIndexer = new CodeIndexer
                {
                    Name = "messageById",
                    SerializationName = "message_id",
                    IndexType = new CodeType
                    {
                        Name = "MessageRequestBuilder"
                    }
                },
                OriginalMethod = new CodeMethod
                {
                    Name = "messageById",
                    Access = AccessModifier.Public,
                    Kind = CodeMethodKind.IndexerBackwardCompatibility,
                    ReturnType = new CodeType
                    {
                        Name = "MessageRequestBuilder"
                    }
                },
                ReturnType = new CodeType
                {
                    Name = "MessageRequestBuilder",
                    IsNullable = false,
                    TypeDefinition = new CodeClass
                    {
                        Name = "MessageRequestBuilder",
                        Kind = CodeClassKind.RequestBuilder,
                        Parent = parentClass.Parent
                    }
                }
            };
            codeMethod.AddParameter(new CodeParameter
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = false
                },
                Kind = CodeParameterKind.Path
            });

            parentClass.AddMethod(codeMethod);
            
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, parentClass.Parent as CodeNamespace);
            languageWriter.Write(codeMethod);
            var result = stringWriter.ToString();

            Assert.Contains("$urlTplParams['message_id'] = $id;", result);
            Assert.Contains("public function messageById(string $id): MessageRequestBuilder {", result);
            Assert.Contains("return new MessageRequestBuilder($urlTplParams, $this->requestAdapter);", result);

        }
        
        public static IEnumerable<object[]> DeserializerProperties => new List<object[]>
        {
            new object[]
            {
                new CodeProperty { Name = "name", Type = new CodeType { Name = "string" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'name' => fn(ParseNode $n) => $o->setName($n->getStringValue()),"
            },
            new object[]
            {
                new CodeProperty { Name = "age", Type = new CodeType { Name = "int32" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'age' => fn(ParseNode $n) => $o->setAge($n->getIntegerValue()),"
            },
            new object[]
            {
                new CodeProperty { Name = "height", Type = new CodeType { Name = "double" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'height' => fn(ParseNode $n) => $o->setHeight($n->getFloatValue()),"
            },
            new object[]
            {
                new CodeProperty { Name = "height", Type = new CodeType { Name = "decimal" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'height' => fn(ParseNode $n) => $o->setHeight($n->getStringValue()),"
            },
            new object[]
            {
                new CodeProperty { Name = "DOB", Type = new CodeType { Name = "DateTimeOffset" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'dOB' => fn(ParseNode $n) => $o->setDOB($n->getDateTimeValue()),"
            },
            new object[]
            {
                new CodeProperty { Name = "story", Type = new CodeType { Name = "binary" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'story' => fn(ParseNode $n) => $o->setStory($n->getBinaryContent()),"
            },
            new object[] { new CodeProperty { Name = "users", Type = new CodeType
                {
                    Name = "EmailAddress", TypeDefinition = new CodeClass
                    {
                        Name = "EmailAddress", Kind = CodeClassKind.Model, 
                        Documentation = new() {
                            Description = "Email",
                        }, Parent = GetParentClassInStaticContext()
                    }, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "'users' => fn(ParseNode $n) => $o->setUsers($n->getCollectionOfObjectValues([EmailAddress::class, 'createFromDiscriminatorValue'])),"
            },
            new object[] { new CodeProperty { Name = "years", Type = new CodeType { Name = "int", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "'years' => fn(ParseNode $n) => $o->setYears($n->getCollectionOfPrimitiveValues())"
            },
            new object[] { new CodeProperty{ Name = "definedInParent", Type = new CodeType { Name = "string"}, OriginalPropertyFromBaseType = new CodeProperty() }, "'definedInParent' => function (ParseNode $n) use ($o) { $o->setDefinedInParent($n->getStringValue())"}
        };
        private static CodeClass GetParentClassInStaticContext()
        {
            CodeClass parent = new CodeClass { Name = "parent" };
            CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();
            rootNamespace.AddClass(parent);
            return parent;
        }
        
        [Theory]
        [MemberData(nameof(DeserializerProperties))]
        public async Task WriteDeserializer(CodeProperty property, string expected)
        {
            parentClass.Kind = CodeClassKind.Model;
            var deserializerMethod = new CodeMethod
            {
                Name = "getDeserializationFields",
                Kind = CodeMethodKind.Deserializer,
                Documentation = new() {
                    Description = "Just some random method",
                },
                ReturnType = new CodeType
                {
                    IsNullable = false,
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "array"
                }
            };
            parentClass.AddProperty(new CodeProperty{
                Name = "noAccessors",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType {
                    Name = "string"
                }
            });
            parentClass.AddMethod(deserializerMethod);
            parentClass.AddProperty(property);
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, parentClass.Parent as CodeNamespace);
            languageWriter.Write(deserializerMethod);
            if (property.ExistsInBaseType)
                Assert.DoesNotContain(expected, stringWriter.ToString());
            else
                Assert.Contains(expected, stringWriter.ToString());
        }

        [Fact]
        public async Task WriteDeserializerMergeWhenHasParent()
        {
            var currentClass = parentClass;
            currentClass.Kind = CodeClassKind.Model;
            var declaration = currentClass.StartBlock;
            declaration.Inherits = new CodeType {Name = "Entity", IsExternal = true, IsNullable = false};
            currentClass.AddProperty(
                new CodeProperty
                {
                    Name = "name",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType {Name = "string"}
                }
            );
            var deserializerMethod = new CodeMethod
            {
                Name = "getDeserializationFields",
                Kind = CodeMethodKind.Deserializer,
                Documentation = new() {
                    Description = "Just some random method",
                },
                ReturnType = new CodeType
                {
                    IsNullable = false,
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "array"
                }
            };
            var cls = new CodeClass
            {
                Name = "ModelParent",
                Kind = CodeClassKind.Model,
                Parent = root,
                StartBlock = new ClassDeclaration { Name = "ModelParent", Parent = root}
            };
            root.AddClass(cls);
            currentClass.StartBlock.Inherits = new CodeType
            {
                TypeDefinition = cls
            };
            currentClass.AddMethod(deserializerMethod);
            
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, parentClass.Parent as CodeNamespace);
            _codeMethodWriter.WriteCodeElement(deserializerMethod, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("array_merge(parent::getFieldDeserializers()", result);
        }

        [Fact]
        public void WriteConstructorBody()
        {
            var constructor = new CodeMethod
            {
                Name = "constructor",
                Access = AccessModifier.Public,
                Documentation = new() {
                    Description = "The constructor for this class",
                },
                ReturnType = new CodeType {Name = "void"},
                Kind = CodeMethodKind.Constructor
            };
            parentClass.AddMethod(constructor);

            var propWithDefaultValue = new CodeProperty
            {
                Name = "type",
                DefaultValue = "\"#microsoft.graph.entity\"",
                Kind = CodePropertyKind.Custom
            };
            parentClass.AddProperty(propWithDefaultValue);

            _codeMethodWriter.WriteCodeElement(constructor, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("public function __construct", result);
            Assert.Contains("$this->setType('#microsoft.graph.entity')", result);
        }
        [Fact]
        public void DoesNotWriteConstructorWithDefaultFromComposedType()
        {
            method.Kind = CodeMethodKind.Constructor;
            var defaultValue = "\"Test Value\"";
            var propName = "size";
            var unionTypeWrapper = root.AddClass(new CodeClass
            {
                Name = "UnionTypeWrapper",
                Kind = CodeClassKind.Model,
                OriginalComposedType = new CodeUnionType
                {
                    Name = "UnionTypeWrapper",
                },
                DiscriminatorInformation = new()
                {
                    DiscriminatorPropertyName = "@odata.type",
                },
            }).First();
            parentClass.AddProperty(new CodeProperty
            {
                Name = propName,
                DefaultValue = defaultValue,
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { TypeDefinition = unionTypeWrapper }
            });
            var sType = new CodeType
            {
                Name = "string",
            };
            var arrayType = new CodeType
            {
                Name = "array",
            };
            unionTypeWrapper.OriginalComposedType.AddType(sType);
            unionTypeWrapper.OriginalComposedType.AddType(arrayType);

            _codeMethodWriter.WriteCodeElement(method, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("__construct()", result);
            Assert.DoesNotContain(defaultValue, result);//ensure the composed type is not referenced
        }
        [Fact]
        public void WriteGetter()
        {
            var getter = new CodeMethod
            {
                Name = "getEmailAddress",
                Documentation = new() {
                    Description = "This method gets the emailAddress",
                },
                ReturnType = new CodeType
                {
                    Name = "emailAddress",
                    IsNullable = false
                },
                Kind = CodeMethodKind.Getter,
                AccessedProperty = new CodeProperty
                {Name = "emailAddress", Access = AccessModifier.Private, Type = new CodeType
                {
                    Name = "emailAddress"
                }},
                Parent = parentClass
            };

            _codeMethodWriter.WriteCodeElement(getter, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains(": EmailAddress {", result);
            Assert.Contains("public function getEmailAddress", result);
        }
        
        [Fact]
        public void WriteGetterAdditionalData()
        {
            var getter = new CodeMethod
            {
                Name = "getAdditionalData",
                Documentation = new() {
                    Description = "This method gets the emailAddress",
                },
                ReturnType = new CodeType
                {
                    Name = "additionalData",
                    IsNullable = false
                },
                Kind = CodeMethodKind.Getter,
                AccessedProperty = new CodeProperty
                {
                    Name = "additionalData", 
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.AdditionalData,
                    Type = new CodeType
                {
                    Name = "additionalData"
                }},
                Parent = parentClass
            };

            _codeMethodWriter.WriteCodeElement(getter, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("public function getAdditionalData(): ?array", result);
            Assert.Contains("return $this->additionalData;", result);
        }

        [Fact]
        public void WriteSetter()
        {
            var setter = new CodeMethod
            {
                Name = "setEmailAddress",
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
                AccessedProperty = new CodeProperty
                {Name = "emailAddress", Access = AccessModifier.Private, Type = new CodeType
                {
                    Name = "emailAddress"
                }},
                Parent = parentClass

            };
            
            setter.AddParameter(new CodeParameter
            {
                Name = "value",
                Kind = CodeParameterKind.SetterValue,
                Type = new CodeType
                {
                    Name = "emailAddress"
                }
            });
            _codeMethodWriter.WriteCodeElement(setter, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("public function setEmailAddress(EmailAddress $value)", result);
            Assert.Contains(": void {", result);
            Assert.Contains("$this->emailAddress = $value", result);
        }

        [Fact]
        public void WriteRequestBuilderWithParametersBody()
        {
            var codeMethod = new CodeMethod
            {
                ReturnType = new CodeType
                {
                    Name = "MessageRequestBuilder",
                    IsNullable = false
                },
                Name = "messageById",
                Parent = parentClass,
                Kind = CodeMethodKind.RequestBuilderWithParameters
            };
            codeMethod.AddParameter(new CodeParameter
            {
                Kind = CodeParameterKind.Path,
                Name = "id",
                Type = new CodeType
                {
                    Name = "string"
                }
            });
            
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("function messageById(string $id): MessageRequestBuilder {", result);
            Assert.Contains("return new MessageRequestBuilder($this->pathParameters, $this->requestAdapter, $id);", result);
        }

        [Fact]
        public void WriteRequestBuilderConstructor()
        {
            method.Kind = CodeMethodKind.Constructor;
            var defaultValue = "[]";
            var propName = "propWithDefaultValue";
            parentClass.Kind = CodeClassKind.RequestBuilder;
            parentClass.AddProperty(new CodeProperty {
                Name = propName,
                DefaultValue = defaultValue,
                Kind = CodePropertyKind.UrlTemplate,
            });
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
            method.AddParameter(new CodeParameter
            {
                Name = "requestAdapter",
                Kind = CodeParameterKind.RequestAdapter,
                Type = new CodeType
                {
                    Name = "RequestAdapter",
                    IsExternal = true
                }
            });
            method.AddParameter(new CodeParameter {
                Name = "pathParameters",
                Kind = CodeParameterKind.PathParameters,
                Type = new CodeType {
                    Name = "array"
                }
            });
            
            method.AddParameter(new CodeParameter
            {
                Kind = CodeParameterKind.Path,
                Name = "username",
                Optional = true,
                Type = new CodeType
                {
                    Name = "string",
                    IsNullable = true
                }
            });

            languageWriter.Write(method);
            var result = stringWriter.ToString();
            Assert.Contains("__construct", result);
            Assert.Contains($"$this->{propName} = {defaultValue};", result);
            Assert.Contains("$this->pathParameters = array_merge($this->pathParameters, $urlTplParams);", result);
        }
        
            [Fact]
    public void WritesModelFactoryBodyForIntersectionModels() {
        var wrapper = AddIntersectionTypeWrapper();
        var factoryMethod = wrapper.AddMethod(new CodeMethod{
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType {
                Name = "IntersectionTypeWrapper",
                TypeDefinition = wrapper,
            },
            IsAsync = false,
            IsStatic = true,
        }).First();
        factoryMethod.AddParameter(new CodeParameter {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType {
                Name = "ParseNode"
            }
        });
        _refiner.Refine(root, new CancellationToken(false));
        languageWriter.Write(factoryMethod);
        var result = stringWriter.ToString();
        Assert.DoesNotContain("$mappingValueNode = $parseNode->getChildNode(\"@odata.type\")", result);
        Assert.DoesNotContain("if ($mappingValueNode != null) {", result);
        Assert.DoesNotContain("$mappingValue = mappingValueNode->getStringValue()", result);
        Assert.DoesNotContain("if mappingValue != null {", result);
        Assert.DoesNotContain("switch (mappingValue) {", result);
        Assert.DoesNotContain("case \"ns.childmodel\": return new ChildModel();", result);
        Assert.Contains("$result = new IntersectionTypeWrapper();", result);
        Assert.Contains("ifs (\"#kiota.complexType1\" === $mappingValue) {", result);
        Assert.Contains("$result->setComplexType1Value(new ComplexType1())", result);
        Assert.Contains("$result->setComplexType3Value(new ComplexType3())", result);
        Assert.Contains("if ($parseNode->getStringValue() !== null) {", result);
        Assert.Contains("$result->setStringValue($parseNode->getStringValue())", result);
        Assert.Contains("else if ($parseNode->getCollectionOfObjectValues([ComplexType2::class, 'createFromDiscriminatorValue']) != null) {", result);
        Assert.Contains("result->setComplexType2Value($parseNode->getCollectionOfObjectValues([ComplexType2::class, 'createFromDiscriminatorValue']))", result);
        Assert.Contains("return $result", result);
        Assert.DoesNotContain("return new IntersectionTypeWrapper()", result);
        AssertExtensions.Before("$parseNode->getStringValue()", "getCollectionOfObjectValues([ComplexType2::class, 'createFromDiscriminatorValue'])", result);
        AssertExtensions.CurlyBracesAreClosed(result);
    }
        
        [Fact]
        public async void WriteFactoryMethod()
        {
            var parentModel = root.AddClass(new CodeClass {
                Name = "parentModel",
                Kind = CodeClassKind.Model,
            }).First();
            var childModel = root.AddClass(new CodeClass {
                Name = "childModel",
                Kind = CodeClassKind.Model,
            }).First();
            childModel.StartBlock.Inherits = new CodeType {
                Name = "parentModel",
                TypeDefinition = parentModel,
            };
            var factoryMethod = parentModel.AddMethod(new CodeMethod {
                Name = "factory",
                Kind = CodeMethodKind.Factory,
                ReturnType = new CodeType {
                    Name = "parentModel",
                    TypeDefinition = parentModel,
                },
                IsStatic = true,
            }).First();
            parentModel.DiscriminatorInformation.AddDiscriminatorMapping("childModel", new CodeType {
                Name = "childModel",
                TypeDefinition = childModel,
            });
            parentModel.DiscriminatorInformation.DiscriminatorPropertyName = "@odata.type";
            factoryMethod.AddParameter(new CodeParameter {
                Name = "ParseNode",
                Kind = CodeParameterKind.ParseNode,
                Type = new CodeType {
                    Name = "ParseNode",
                    TypeDefinition = new CodeClass {
                        Name = "ParseNode",
                    },
                    IsExternal = true,
                },
                Optional = false,
            });
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, parentClass.Parent as CodeNamespace);
            languageWriter.Write(factoryMethod);
            var result = stringWriter.ToString();
            Assert.Contains("case 'childModel': return new ChildModel();", result);
            Assert.Contains("$mappingValueNodes = $parseNode->getChildNode(\"@odata.type\");", result);
        }
        [Fact]
        public async void WriteApiConstructor()
        {
            parentClass.AddProperty(new CodeProperty
            {
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
                Type = new CodeType {Name = "RequestAdapter"}
            });
            var codeMethod = new CodeMethod
            {
                ReturnType = new CodeType
                {
                    Name = "void",
                    IsNullable = false
                },
                Name = "construct",
                Parent = parentClass,
                Kind = CodeMethodKind.ClientConstructor
            };
            codeMethod.BaseUrl = "https://graph.microsoft.com/v1.0";
            parentClass.AddProperty(new CodeProperty {
                Name = "pathParameters",
                Kind = CodePropertyKind.PathParameters,
                Type = new CodeType {
                    Name = "array",
                    IsExternal = true,
                }
            });

            codeMethod.AddParameter(new CodeParameter
            {
                Kind = CodeParameterKind.RequestAdapter,
                Name = "requestAdapter",
                Type = new CodeType
                {
                    Name = "RequestAdapter"
                },
                SerializationName = "rawUrl"
            });
            codeMethod.DeserializerModules = new() {"Microsoft\\Kiota\\Serialization\\Deserializer"};
            codeMethod.SerializerModules = new() {"Microsoft\\Kiota\\Serialization\\Serializer"};
            parentClass.AddMethod(codeMethod);
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP}, parentClass.Parent as CodeNamespace);
            languageWriter.Write(codeMethod);
            var result = stringWriter.ToString();
            Assert.Contains("$this->requestAdapter = $requestAdapter", result);
            Assert.Contains("public function __construct(RequestAdapter $requestAdapter)", result);
            Assert.Contains($"$this->pathParameters['baseUrl'] = $this->requestAdapter->getBaseUrl();", result);
        }

        [Fact]
        public async void WritesApiClientWithBackingStoreConstructor()
        {
            var constructor = new CodeMethod { 
                Name = "construct", 
                Kind = CodeMethodKind.ClientConstructor, 
                ReturnType = new CodeType
                {
                    Name = "void",
                    IsNullable = false
                }
            };
            constructor.DeserializerModules = new() {"Microsoft\\Kiota\\Serialization\\Deserializer"};
            constructor.SerializerModules = new() {"Microsoft\\Kiota\\Serialization\\Serializer"};
            var parameter = new CodeParameter
            {
                Name = "backingStore",
                Optional = true,
                Documentation = new(){
                    Description = "The backing store to use for the models.",
                },
                Kind = CodeParameterKind.BackingStore,
                Type = new CodeType
                {
                    Name = "IBackingStoreFactory", IsNullable = true,
                }
            };
            
            parentClass.AddProperty(new CodeProperty
            {
                Name = "requestAdapter",
                Kind = CodePropertyKind.RequestAdapter,
                Type = new CodeType {Name = "RequestAdapter"}
            });
            constructor.AddParameter(new CodeParameter
            {
                Kind = CodeParameterKind.RequestAdapter,
                Name = "requestAdapter",
                Type = new CodeType
                {
                    Name = "RequestAdapter"
                },
                SerializationName = "rawUrl"
            });
            
            constructor.AddParameter(parameter);
            parentClass.AddMethod(constructor);
            parentClass.Kind = CodeClassKind.RequestBuilder;
            
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP, UsesBackingStore = true}, root);
            _codeMethodWriter = new CodeMethodWriter(new PhpConventionService(), true);
            _codeMethodWriter.WriteCodeElement(constructor, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("public function __construct(RequestAdapter $requestAdapter, ?BackingStoreFactory $backingStore = null)", result);
            Assert.Contains("$this->requestAdapter->enableBackingStore($backingStore ?? BackingStoreFactorySingleton::getInstance());", result);
        }

        [Fact]
        public async void WritesModelWithBackingStoreConstructor()
        {
            parentClass.Kind = CodeClassKind.Model;
            var constructor = new CodeMethod
            {
                Name = "constructor",
                Access = AccessModifier.Public,
                Documentation = new() {
                    Description = "The constructor for this class",
                },
                ReturnType = new CodeType {Name = "void"},
                Kind = CodeMethodKind.Constructor
            };
            parentClass.AddMethod(constructor);

            var propWithDefaultValue = new CodeProperty
            {
                Name = "backingStore",
                Access = AccessModifier.Public,
                DefaultValue = "BackingStoreFactorySingleton.Instance.CreateBackingStore()",
                Kind = CodePropertyKind.BackingStore,
                Type = new CodeType { Name = "IBackingStore", IsExternal = true, IsNullable = false }
            };
            parentClass.AddProperty(propWithDefaultValue);
            
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP, UsesBackingStore = true}, root);
            _codeMethodWriter = new CodeMethodWriter(new PhpConventionService(), true);
            _codeMethodWriter.WriteCodeElement(constructor, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains("$this->backingStore = BackingStoreFactorySingleton::getInstance()->createBackingStore();", result);
        }

        [Fact]
        public async void WritesGettersAndSettersWithBackingStore()
        {
            parentClass.Kind = CodeClassKind.Model;
            var backingStoreProperty = new CodeProperty
            {
                Name = "backingStore",
                Access = AccessModifier.Public,
                DefaultValue = "BackingStoreFactorySingleton.Instance.CreateBackingStore()",
                Kind = CodePropertyKind.BackingStore,
                Type = new CodeType { Name = "IBackingStore", IsExternal = true, IsNullable = false }
            };
            parentClass.AddProperty(backingStoreProperty);
            var modelProperty = new CodeProperty
            {
                Name = "name",
                Access = AccessModifier.Public,
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "string", IsNullable = true }
            };
            parentClass.AddProperty(modelProperty);
            
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP, UsesBackingStore = true}, root);
            _codeMethodWriter = new CodeMethodWriter(new PhpConventionService(), true);
            // Refiner adds setters & getters for properties
            foreach (var getter in parentClass.GetMethodsOffKind(CodeMethodKind.Getter, CodeMethodKind.Setter))
            {
                _codeMethodWriter.WriteCodeElement(getter, languageWriter);
            }
            var result = stringWriter.ToString();

            Assert.Contains("public function getName(): ?string", result);
            Assert.Contains("return $this->getBackingStore()->get('name');", result);

            Assert.Contains("public function getBackingStore(): BackingStore", result);
            Assert.Contains("return $this->backingStore;", result);

            Assert.Contains("public function setName(?string $value)", result);
            Assert.Contains("$this->getBackingStore()->set('name', $value);", result);
            
            Assert.Contains("public function setBackingStore(BackingStore $value)", result);
            Assert.Contains("$this->backingStore = $value;", result);
        }

        [Fact]
        public async void ReplaceBinaryTypeWithStreamInterface()
        {
            var binaryProperty = new CodeProperty
            {
                Name = "binaryContent",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType { Name = "binary", IsNullable = false }
            };
            parentClass.AddProperty(binaryProperty);
            await ILanguageRefiner.Refine(new GenerationConfiguration {Language = GenerationLanguage.PHP, UsesBackingStore = true}, root);
            parentClass.GetMethodsOffKind(CodeMethodKind.Getter, CodeMethodKind.Setter).ToList().ForEach(x => _codeMethodWriter.WriteCodeElement(x, languageWriter));
            var result = stringWriter.ToString();

            Assert.Contains("public function setBinaryContent(?StreamInterface $value): void", result);
            Assert.Contains("public function getBinaryContent(): ?StreamInterface", result);
        }
        private CodeClass AddIntersectionTypeWrapper() {
            var complexType1 = root.AddClass(new CodeClass
            {
                Name = "ComplexType1",
                Kind = CodeClassKind.Model,
            }).First();
            var complexType2 = root.AddClass(new CodeClass
            {
                Name = "ComplexType2",
                Kind = CodeClassKind.Model,
            }).First();
            var complexType3 = root.AddClass(new CodeClass
            {
                Name = "ComplexType3",
                Kind = CodeClassKind.Model,
            }).First();
            var intersectionTypeWrapper = root.AddClass(new CodeClass
            {
                Name = "IntersectionTypeWrapper",
                Kind = CodeClassKind.Model,
                OriginalComposedType = new CodeIntersectionType {
                    Name = "IntersectionTypeWrapper",
                },
                DiscriminatorInformation = new() {
                    DiscriminatorPropertyName = "@odata.type",
                },
            }).First();
            var cType1 = new CodeType {
                Name = "ComplexType1",
                TypeDefinition = complexType1
            };
            var cType2 = new CodeType {
                Name = "ComplexType2",
                TypeDefinition = complexType2,
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
            };
            var cType3 = new CodeType {
                Name = "ComplexType3",
                TypeDefinition = complexType3
            };
            intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType {
                Name = "ComplexType1",
                TypeDefinition = cType1
            });
            intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType {
                Name = "ComplexType2",
                TypeDefinition = cType2
            });
            intersectionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType3", new CodeType {
                Name = "ComplexType3",
                TypeDefinition = cType3
            });
            var sType = new CodeType {
                Name = "string",
            };
            intersectionTypeWrapper.OriginalComposedType.AddType(cType1);
            intersectionTypeWrapper.OriginalComposedType.AddType(cType2);
            intersectionTypeWrapper.OriginalComposedType.AddType(cType3);
            intersectionTypeWrapper.OriginalComposedType.AddType(sType);
            intersectionTypeWrapper.AddProperty(new CodeProperty {
                Name = "ComplexType1Value",
                Type = cType1,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetComplexType1Value",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetComplexType1Value",
                    ReturnType = cType1,
                    Kind = CodeMethodKind.Getter,
                }
            });
            intersectionTypeWrapper.AddProperty(new CodeProperty {
                Name = "ComplexType2Value",
                Type = cType2,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetComplexType2Value",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetComplexType2Value",
                    ReturnType = cType2,
                    Kind = CodeMethodKind.Getter,
                }
            });
            intersectionTypeWrapper.AddProperty(new CodeProperty {
                Name = "ComplexType3Value",
                Type = cType3,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetComplexType3Value",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetComplexType3Value",
                    ReturnType = cType3,
                    Kind = CodeMethodKind.Getter,
                }
            });
            intersectionTypeWrapper.AddProperty(new CodeProperty {
                Name = "StringValue",
                Type = sType,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetStringValue",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetStringValue",
                    ReturnType = sType,
                    Kind = CodeMethodKind.Getter,
                }
            });
            return intersectionTypeWrapper;
        }
        private CodeClass AddUnionTypeWrapper() {
            var complexType1 = root.AddClass(new CodeClass
            {
                Name = "ComplexType1",
                Kind = CodeClassKind.Model,
            }).First();
            var complexType2 = root.AddClass(new CodeClass
            {
                Name = "ComplexType2",
                Kind = CodeClassKind.Model,
            }).First();
            var unionTypeWrapper = root.AddClass(new CodeClass
            {
                Name = "UnionTypeWrapper",
                Kind = CodeClassKind.Model,
                OriginalComposedType = new CodeUnionType {
                    Name = "UnionTypeWrapper",
                },
                DiscriminatorInformation = new() {
                    DiscriminatorPropertyName = "@odata.type",
                },
            }).First();
            var cType1 = new CodeType {
                Name = "ComplexType1",
                TypeDefinition = complexType1
            };
            var cType2 = new CodeType {
                Name = "ComplexType2",
                TypeDefinition = complexType2,
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
            };
            var sType = new CodeType {
                Name = "string",
            };
            unionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType1", new CodeType {
                Name = "ComplexType1",
                TypeDefinition = cType1
            });
            unionTypeWrapper.DiscriminatorInformation.AddDiscriminatorMapping("#kiota.complexType2", new CodeType {
                Name = "ComplexType2",
                TypeDefinition = cType2
            });
            unionTypeWrapper.OriginalComposedType.AddType(cType1);
            unionTypeWrapper.OriginalComposedType.AddType(cType2);
            unionTypeWrapper.OriginalComposedType.AddType(sType);
            unionTypeWrapper.AddProperty(new CodeProperty {
                Name = "ComplexType1Value",
                Type = cType1,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetComplexType1Value",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetComplexType1Value",
                    ReturnType = cType1,
                    Kind = CodeMethodKind.Getter,
                }
            });
            unionTypeWrapper.AddProperty(new CodeProperty {
                Name = "ComplexType2Value",
                Type = cType2,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetComplexType2Value",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetComplexType2Value",
                    ReturnType = cType2,
                    Kind = CodeMethodKind.Getter,
                }
            });
            unionTypeWrapper.AddProperty(new CodeProperty {
                Name = "StringValue",
                Type = sType,
                Kind = CodePropertyKind.Custom,
                Setter = new CodeMethod {
                    Name = "SetStringValue",
                    ReturnType = new CodeType {
                        Name = "void"
                    },
                    Kind = CodeMethodKind.Setter,
                },
                Getter = new CodeMethod {
                    Name = "GetStringValue",
                    ReturnType = sType,
                    Kind = CodeMethodKind.Getter,
                }
            });
            return unionTypeWrapper; 
        }
    }
}
