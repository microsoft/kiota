using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
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
        private readonly CodeMethodWriter _codeMethodWriter;
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
                Description = "This is a very good method to try all the good things"
            };
            method.ReturnType = new CodeType
            {
                Name = ReturnTypeName
            };
            _refiner = new PhpRefiner(new GenerationConfiguration {Language = GenerationLanguage.PHP});
            parentClass.AddMethod(method);
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
            var codeMethod = new CodeMethod
            {
                Access = AccessModifier.Public,
                Kind = CodeMethodKind.Custom,
                ReturnType = new CodeType
                {
                    Name = "void"
                },
                Parent = parentClass
            };
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
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
                Description = "This will send a POST request",
                Kind = CodeMethodKind.RequestExecutor
            };
            codeMethod.AddParameter(new CodeParameter
            {
                Name = "ResponseHandler",
                Kind = CodeParameterKind.ResponseHandler,
                Optional = true,
                Type = new CodeType
                {
                    Name = "ResponseHandler",
                    IsNullable = true
                }
            });
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
            Assert.Contains("catch(Exception $ex)", result);
            Assert.Contains("'403' => array(Error403::class, 'createFromDiscriminatorValue')", result);
            Assert.Contains("return $this->requestAdapter->sendPrimitiveAsync($requestInfo, StreamInterface::class, $responseHandler, $errorMappings);", result);
        }
        
        public static IEnumerable<object[]> SerializerProperties => new List<object[]>
        {
            new object[]
            {
                new CodeProperty { Name = "name", Type = new CodeType { Name = "string" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeStringValue('name', $this->name);"
            },
            new object[]
            {
                new CodeProperty { Name = "email", Type = new CodeType
                {
                    Name = "EmailAddress", TypeDefinition = new CodeClass { Name = "EmailAddress", Kind = CodeClassKind.Model}
                }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeObjectValue('email', $this->email);"
            },
            new object[]
            {
                new CodeProperty { Name = "status", Type = new CodeType { Name = "Status", TypeDefinition = new CodeEnum
                {
                    Name = "Status", Description = "Status Enum"
                }}, Access = AccessModifier.Private },
                "$writer->writeEnumValue('status', $this->status);"
            },
            new object[]
            {
                new CodeProperty { Name = "architectures", Type = new CodeType
                {
                    Name = "Architecture", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array, TypeDefinition = new CodeEnum { Name = "Architecture", Description = "Arch Enum, accepts x64, x86, hybrid"}
                }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "$writer->writeCollectionOfEnumValues('architectures', $this->architectures);"
            },
            new object[] { new CodeProperty { Name = "emails", Type = new CodeType
            {
                Name = "Email", TypeDefinition = new CodeClass { Name = "Email", Kind = CodeClassKind.Model}, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "$writer->writeCollectionOfObjectValues('emails', $this->emails);"
            },
            new object[] { new CodeProperty { Name = "temperatures", Type = new CodeType { Name = "int", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "$writer->writeCollectionOfPrimitiveValues('temperatures', $this->temperatures);"
            },
            // Primitive int tests
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "integer" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->age);" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "int32" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->age);" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "int64" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->age);" },
            new object[] { new CodeProperty { Name = "age", Type = new CodeType { Name = "sbyte" }, Access = AccessModifier.Private}, "$writer->writeIntegerValue('age', $this->age);" },
            // Float tests
            new object[] { new CodeProperty { Name = "height", Type = new CodeType { Name = "float" }, Access = AccessModifier.Private}, "$writer->writeFloatValue('height', $this->height);" },
            new object[] { new CodeProperty { Name = "height", Type = new CodeType { Name = "double" }, Access = AccessModifier.Private}, "$writer->writeFloatValue('height', $this->height);" },
            // Bool tests
            new object[] { new CodeProperty { Name = "married", Type = new CodeType { Name = "boolean" }, Access = AccessModifier.Private}, "$writer->writeBooleanValue('married', $this->married);" },
            new object[] { new CodeProperty { Name = "slept", Type = new CodeType { Name = "bool" }, Access = AccessModifier.Private}, "$writer->writeBooleanValue('slept', $this->slept);" },
            // Decimal and byte tests
            new object[] { new CodeProperty { Name = "money", Type = new CodeType { Name = "decimal" }, Access = AccessModifier.Private}, "$writer->writeStringValue('money', $this->money);" },
            new object[] { new CodeProperty { Name = "money", Type = new CodeType { Name = "byte" }, Access = AccessModifier.Private}, "$writer->writeStringValue('money', $this->money);" },
            new object[] { new CodeProperty { Name = "dateValue", Type = new CodeType { Name = "DateTime" }, Access = AccessModifier.Private}, "$writer->writeDateTimeValue('dateValue', $this->dateValue);" },
            new object[] { new CodeProperty { Name = "duration", Type = new CodeType { Name = "duration" }, Access = AccessModifier.Private}, "$writer->writeDateIntervalValue('duration', $this->duration);" },
            new object[] { new CodeProperty { Name = "stream", Type = new CodeType { Name = "binary" }, Access = AccessModifier.Private}, "$writer->writeBinaryContent('stream', $this->stream);" },
            new object[] { new CodeProperty { Name = "definedInParent", Type = new CodeType { Name = "string"}, OriginalPropertyFromBaseType = new CodeProperty() }, "$write->writeStringValue('definedInParent', $this->definedInParent);"}
        };
        
        [Theory]
        [MemberData(nameof(SerializerProperties))]
        public void WriteSerializer(CodeProperty property, string expected)
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
            parentClass.Kind = CodeClassKind.Model;
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
            var result = stringWriter.ToString();
            Assert.Contains("public function serialize(SerializationWriter $writer)", result);
            if (property.ExistsInBaseType)
                Assert.DoesNotContain(expected, result);
            else
                Assert.Contains(expected, stringWriter.ToString());
        }

        [Fact]
        public void WriteRequestGenerator()
        {
            parentClass.Kind = CodeClassKind.RequestBuilder;
            parentClass.AddProperty(
                new CodeProperty
                {
                    Name = "urlTemplate",
                    Access = AccessModifier.Protected,
                    DefaultValue = "https://graph.microsoft.com/v1.0/",
                    Description = "The URL template",
                    Kind = CodePropertyKind.UrlTemplate,
                    Type = new CodeType {Name = "string"}
                },
                new CodeProperty
                {
                    Name = "pathParameters",
                    Access = AccessModifier.Protected,
                    DefaultValue = "[]",
                    Description = "The Path parameters.",
                    Kind = CodePropertyKind.PathParameters,
                    Type = new CodeType {Name = "array"}
                },
                new CodeProperty
                {
                    Name = "requestAdapter",
                    Access = AccessModifier.Protected,
                    Description = "The request Adapter",
                    Kind = CodePropertyKind.RequestAdapter,
                    Type = new CodeType
                    {
                        IsNullable = false,
                        Name = "RequestAdapter"
                    }
                });
            var codeMethod = new CodeMethod
            {
                Name = "createPostRequestInformation",
                ReturnType = new CodeType {Name = "RequestInformation", IsNullable = false},
                Access = AccessModifier.Public,
                Description = "This method creates request information for POST request.",
                HttpMethod = HttpMethod.Post,
                BaseUrl = "https://graph.microsoft.com/v1.0/",
                Kind = CodeMethodKind.RequestGenerator,
            };

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
            });
            
            codeMethod.AddParameter(
                new CodeParameter
                {
                    Name = "body",
                    Kind = CodeParameterKind.RequestBody,
                    Type = new CodeType
                    {
                        Name = "Message",
                        IsExternal = true,
                        IsNullable = false
                    }
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
                });

            
            parentClass.AddMethod(codeMethod);
            
            _codeMethodWriter.WriteCodeElement(codeMethod, languageWriter);
            var result = stringWriter.ToString();

            Assert.Contains(
                "public function createPostRequestInformation(Message $body, ?RequestConfig $requestConfiguration = null): RequestInformation",
                result);
            Assert.Contains("if ($requestConfiguration !== null", result);
            Assert.Contains("if ($requestConfiguration->h !== null)", result);
            Assert.Contains("$requestInfo->headers = array_merge($requestInfo->headers, $requestConfiguration->h);", result);
            Assert.Contains("$requestInfo->setQueryParameters($requestConfiguration->q);", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$requestConfiguration->o);", result);
            Assert.Contains("return $requestInfo;", result);
        }

        [Fact]
        public void WriteIndexerBody()
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
                Description = "Get messages by a specific ID.",
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
            
            _refiner.Refine(parentClass.Parent as CodeNamespace);
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
                "'name' => function (ParseNode $n) use ($o) { $o->setName($n->getStringValue()); },"
            },
            new object[]
            {
                new CodeProperty { Name = "age", Type = new CodeType { Name = "int32" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'age' => function (ParseNode $n) use ($o) { $o->setAge($n->getIntegerValue()); },"
            },
            new object[]
            {
                new CodeProperty { Name = "height", Type = new CodeType { Name = "double" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'height' => function (ParseNode $n) use ($o) { $o->setHeight($n->getFloatValue()); },"
            },
            new object[]
            {
                new CodeProperty { Name = "height", Type = new CodeType { Name = "decimal" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'height' => function (ParseNode $n) use ($o) { $o->setHeight($n->getStringValue()); },"
            },
            new object[]
            {
                new CodeProperty { Name = "DOB", Type = new CodeType { Name = "DateTimeOffset" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'dOB' => function (ParseNode $n) use ($o) { $o->setDOB($n->getDateTimeValue()); },"
            },
            new object[]
            {
                new CodeProperty { Name = "story", Type = new CodeType { Name = "binary" }, Access = AccessModifier.Private, Kind = CodePropertyKind.Custom },
                "'story' => function (ParseNode $n) use ($o) { $o->setStory($n->getBinaryContent()); },"
            },
            new object[] { new CodeProperty { Name = "users", Type = new CodeType
                {
                    Name = "EmailAddress", TypeDefinition = new CodeClass
                    {
                        Name = "EmailAddress", Kind = CodeClassKind.Model, Description = "Email", Parent = GetParentClassInStaticContext()
                    }, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "'users' => function (ParseNode $n) use ($o) { $o->setUsers($n->getCollectionOfObjectValues(array(EmailAddress::class, 'createFromDiscriminatorValue')));"
            },
            new object[] { new CodeProperty { Name = "years", Type = new CodeType { Name = "int", CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array }, Access = AccessModifier.Private},
                "'years' => function (ParseNode $n) use ($o) { $o->setYears($n->getCollectionOfPrimitiveValues())"
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
        public void WriteDeserializer(CodeProperty property, string expected)
        {
            parentClass.Kind = CodeClassKind.Model;
            var deserializerMethod = new CodeMethod
            {
                Name = "getDeserializationFields",
                Kind = CodeMethodKind.Deserializer,
                Description = "Just some random method",
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
            _refiner.Refine(parentClass.Parent as CodeNamespace);
            languageWriter.Write(deserializerMethod);
            if (property.ExistsInBaseType)
                Assert.DoesNotContain(expected, stringWriter.ToString());
            else
                Assert.Contains(expected, stringWriter.ToString());
        }

        [Fact]
        public void WriteDeserializerMergeWhenHasParent()
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
                Description = "Just some random method",
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
            
            _refiner.Refine(parentClass.Parent as CodeNamespace);
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
                Description = "The constructor for this class",
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
        public void WriteGetter()
        {
            var getter = new CodeMethod
            {
                Name = "getEmailAddress",
                Description = "This method gets the emailAddress",
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
                Description = "This method gets the emailAddress",
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
            Assert.Contains("public function getAdditionalData(): array", result);
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
        public void WriteFactoryMethod()
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
            _refiner.Refine(parentClass.Parent as CodeNamespace);
            languageWriter.Write(factoryMethod);
            var result = stringWriter.ToString();
            Assert.Contains("case 'childModel': return new ChildModel();", result);
            Assert.Contains("$mappingValueNode = $parseNode->getChildNode(\"@odata.type\");", result);
        }
        [Fact]
        public void WriteApiConstructor()
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
            _refiner.Refine(parentClass.Parent as CodeNamespace);
            languageWriter.Write(codeMethod);
            var result = stringWriter.ToString();
            Assert.Contains("$this->requestAdapter = $requestAdapter", result);
            Assert.Contains("public function __construct(RequestAdapter $requestAdapter)", result);
        }
    }
}
