using System;
using System.IO;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Php;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeMethodWriterTests: IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private readonly CodeClass parentClass;
        private const string MethodName = "methodName";
        private const string ReturnTypeName = "Promise";
        private const string MethodDescription = "some description";
        private const string ParamDescription = "some parameter description";
        private const string ParamName = "paramName";
        private readonly CodeMethodWriter _codeMethodWriter;

        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            root.Name = "Microsoft\\Graph";
            _codeMethodWriter = new CodeMethodWriter(new PhpConventionService());
            parentClass = new CodeClass() {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod() {
                Name = MethodName,
                IsAsync = true,
                Description = "This is a very good method to try all the good things"
            };
            method.ReturnType = new CodeType() {
                Name = ReturnTypeName
            };
            parentClass.AddMethod(method);
        }
        [Fact]
        public void WriteABasicMethod()
        {
            var declaration = method;
            _codeMethodWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("public function", result);
        }

        [Fact]
        public void WriteMethodWithNoDescription()
        {
            var codeMethod = new CodeMethod()
            {
                Access = AccessModifier.Public,
                Kind = CodeMethodKind.Custom,
                ReturnType = new CodeType()
                {
                    Name = "void"
                },
                Parent = parentClass
            };
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();
            
            Assert.DoesNotContain("/*", result);
        }

        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void WriteRequestExecutor()
        {
            var codeClass = parentClass;
            codeClass.AddProperty(new CodeProperty()
            {
                Kind = CodePropertyKind.RequestAdapter, Name = "requestAdapter"
            });
            codeClass.AddProperty(new CodeProperty()
            {
                Kind = CodePropertyKind.UrlTemplate, Name = "urlTemplate"
            });
            codeClass.AddProperty(new CodeProperty()
            {
                Kind = CodePropertyKind.PathParameters, Name = "pathParameters"
            });
            var codeMethod = new CodeMethod()
            {
                Name = "get",
                HttpMethod = HttpMethod.Post,
                ReturnType = new CodeType()
                {
                    IsExternal = true,
                    Name = "returnType"
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
            var codeMethodRequestGenerator = new CodeMethod()
            {
                Kind = CodeMethodKind.RequestGenerator,
                HttpMethod = HttpMethod.Post,
                Name = "createPostRequestInformation",
                ReturnType = new CodeType()
                {
                    Name = "RequestInformation"
                }
            };
            codeClass.AddMethod(codeMethod);
            codeClass.AddMethod(codeMethodRequestGenerator);
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();

            Assert.Contains("Promise", result);
            Assert.Contains("$requestInfo = $this->createPostRequestInformation();", result);
            Assert.Contains("RejectedPromise", result);
            Assert.Contains("catch(Exception $ex)", result);
        }
        
        [Fact]
        public void WriteSerializer()
        {
            var classHolding = parentClass;
            classHolding.Kind = CodeClassKind.Model;
            classHolding.AddProperty(
                new CodeProperty()
                {
                    Type = new CodeType()
                    {
                        Name = "string"
                    },
                    Name = "name",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom
                });
            classHolding.AddProperty(
                new CodeProperty()
                {
                    Name = "email",
                    Access = AccessModifier.Private,
                    Type = new CodeType()
                    {
                        Name = "EmailAddress",
                        TypeDefinition = new CodeClass()
                        {
                            Name = "EmailAddress",
                            Kind = CodeClassKind.Model
                        }
                    },
                    Kind = CodePropertyKind.Custom
                });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "status",
                Access = AccessModifier.Private,
                Type = new CodeType
                {
                    Name = "Status",
                    TypeDefinition = new CodeEnum {Name = "Status", Description = "Status Enum"}
                }
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "architectures",
                Access = AccessModifier.Private,
                Type = new CodeType
                {
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "Architecture",
                    TypeDefinition = new CodeEnum {Name = "Architecture", Description = "Arch Enum, accepts x64, x86, hybrid"}
                }
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "age", Access = AccessModifier.Private, Type = new CodeType {Name = "int"}
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "height", Access = AccessModifier.Private, Type = new CodeType {Name = "float"}
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "married", Access = AccessModifier.Private, Type = new CodeType {Name = "boolean"}
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "slept", Access = AccessModifier.Private, Type = new CodeType {Name = "bool"}
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "emails",
                Access = AccessModifier.Private,
                Type = new CodeType
                {
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "Email", TypeDefinition = new CodeClass {Name = "Email", Kind = CodeClassKind.Model}
                }
            });
            classHolding.AddProperty(new CodeProperty
            {
                Name = "temperatures",
                Access = AccessModifier.Private,
                Type = new CodeType
                {
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "int"
                }
            });
            var codeMethod = new CodeMethod()
            {
                Name = "serialize",
                Kind = CodeMethodKind.Serializer,
                ReturnType = new CodeType()
                {
                    Name = "void",
                }
            };
            codeMethod.AddParameter(new CodeParameter()
            {
                Name = "writer",
                Kind = CodeParameterKind.Serializer,
                Type = new CodeType()
                {
                    Name = "SerializationWriter"
                }
            });
            classHolding.AddMethod(codeMethod);
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();

            Assert.Contains("public function serialize(", result);
            Assert.Contains("$writer->writeStringValue('name', $this->name);", result);
            Assert.Contains("$writer->writeObjectValue('email', $this->email);", result);
            Assert.Contains("$writer->writeIntegerValue('age', $this->age", result);
            Assert.Contains("$writer->writeCollectionOfEnumValues('architectures', $this->architectures);",result);
            Assert.Contains("$writer->writeObjectValue('email', $this->email);", result);
            Assert.Contains("$writer->writeCollectionOfObjectValues('emails', $this->emails);", result);
            Assert.Contains("$writer->writeFloatValue('height', $this->height);", result);
            Assert.Contains("$writer->writeBooleanValue('married', $this->married);", result);
            Assert.Contains("$writer->writeStringValue('name', $this->name);", result);
            Assert.Contains("$writer->writeBooleanValue('slept', $this->slept);", result);
            Assert.Contains("$writer->writeEnumValue('status', $this->status);", result);
            Assert.Contains("$writer->writeCollectionOfPrimitiveValues('temperatures', $this->temperatures);", result);
        }

        [Fact]
        public void WriteRequestGenerator()
        {
            var methodClass = parentClass;
            methodClass.Kind = CodeClassKind.RequestBuilder;
            methodClass.AddProperty(
                new CodeProperty()
                {
                    Name = "urlTemplate",
                    Access = AccessModifier.Protected,
                    DefaultValue = "https://graph.microsoft.com/v1.0/",
                    Description = "The URL template",
                    Kind = CodePropertyKind.UrlTemplate,
                    Type = new CodeType() {Name = "string"}
                },
                new CodeProperty()
                {
                    Name = "pathParameters",
                    Access = AccessModifier.Protected,
                    DefaultValue = "[]",
                    Description = "The Path parameters.",
                    Kind = CodePropertyKind.PathParameters,
                    Type = new CodeType() {Name = "array"}
                },
                new CodeProperty()
                {
                    Name = "requestAdapter",
                    Access = AccessModifier.Protected,
                    Description = "The request Adapter",
                    Kind = CodePropertyKind.RequestAdapter,
                    Type = new CodeType()
                    {
                        IsNullable = false,
                        Name = "RequestAdapter"
                    }
                });
            var codeMethod = new CodeMethod()
            {
                Name = "createPostRequestInformation",
                ReturnType = new CodeType() {Name = "RequestInformation", IsNullable = false},
                Access = AccessModifier.Public,
                Description = "This method creates request information for POST request.",
                HttpMethod = HttpMethod.Post,
                BaseUrl = "https://graph.microsoft.com/v1.0/",
                Kind = CodeMethodKind.RequestGenerator,
            };
            
            codeMethod.AddParameter(
                new CodeParameter()
                {
                    Name = "body",
                    Kind = CodeParameterKind.RequestBody,
                    Type = new CodeType()
                    {
                        Name = "Message",
                        IsExternal = true,
                        IsNullable = false
                    }
                },
                new CodeParameter
                {
                    Optional = true,
                    Name = "headers",
                    Kind = CodeParameterKind.Headers,
                    Type = new CodeType()
                    {
                        Name = "array"
                    }
                
                },
                new CodeParameter
                {
                    Optional = true,
                    Name = "options",
                    Kind = CodeParameterKind.Options,
                    Type = new CodeType
                    {
                        Name = "array"
                    }
                }, new CodeParameter
                {
                    Optional = true,
                    Name = "queryString",
                    Kind = CodeParameterKind.QueryParameter,
                    Type = new CodeType
                    {
                        Name = "array",
                        IsNullable = true
                    }
                });

            
            methodClass.AddMethod(codeMethod);
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();

            Assert.Contains(
                "public function createPostRequestInformation(Message $body, ?array $queryParameters = null, ?array $headers = null, ?array $options = null): RequestInformation",
                result);
            Assert.Contains("return $requestInfo;", result);
            Assert.Contains("$requestInfo->addRequestOptions(...$options);", result);
        }

        [Fact]
        public void WriteIndexerBody()
        {
            var currentClass = parentClass;

            currentClass.AddProperty(
                new CodeProperty()
                {
                    Name = "pathParameters",
                    Kind = CodePropertyKind.PathParameters,
                    Type = new CodeType() {Name = "array"}
                },
                new CodeProperty()
                {
                    Name = "requestAdapter",
                    Kind = CodePropertyKind.RequestAdapter,
                    Type = new CodeType()
                    {
                        Name = "requestAdapter"
                    }
                }
            );
            var codeMethod = new CodeMethod()
            {
                Name = "messageById",
                Access = AccessModifier.Public,
                Kind = CodeMethodKind.IndexerBackwardCompatibility,
                Description = "Get messages by a specific ID.",
                OriginalIndexer = new CodeIndexer()
                {
                    Name = "messageById",
                    ParameterName = "message_id",
                    IndexType = new CodeType()
                    {
                        Name = "MessageRequestBuilder"
                    }
                },
                OriginalMethod = new CodeMethod()
                {
                    Name = "messageById",
                    Access = AccessModifier.Public,
                    Kind = CodeMethodKind.IndexerBackwardCompatibility,
                    ReturnType = new CodeType()
                    {
                        Name = "MessageRequestBuilder"
                    }
                },
                ReturnType = new CodeType()
                {
                    Name = "MessageRequestBuilder",
                    IsNullable = false,
                    TypeDefinition = new CodeClass()
                    {
                        Name = "MessageRequestBuilder",
                        Kind = CodeClassKind.RequestBuilder,
                    }
                }
            };
            codeMethod.AddParameter(new CodeParameter()
            {
                Name = "id",
                Type = new CodeType()
                {
                    Name = "string",
                    IsNullable = false
                }
            });

            currentClass.AddMethod(codeMethod);
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();

            Assert.Contains("$urlTplParams['message_id'] = $id;", result);
            Assert.Contains("public function messageById(string $id): MessageRequestBuilder {", result);
            Assert.Contains("return new MessageRequestBuilder($urlTplParams, $this->requestAdapter);", result);

        }

        [Fact]
        public void WriteDeserializer()
        {
            var currentClass = parentClass;
            currentClass.Kind = CodeClassKind.Model;
            currentClass.AddProperty(
                new CodeProperty()
                {
                    Name = "name",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType() {Name = "string"}
                },
                new CodeProperty
                {
                    Name = "users",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                        Name = "EmailAddress",
                        TypeDefinition = new CodeClass
                        {
                            Name = "EmailAddress",
                            Description = "Email",
                            Kind = CodeClassKind.Model
                        }
                    }
                },
                new CodeProperty
                {
                    Name = "years",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                        Name = "int"
                    }
                },
                new CodeProperty
                {
                    Name = "archs",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                        Name = "Arch",
                        TypeDefinition = new CodeEnum
                        {
                            Name = "Arch"
                        }
                    }
                },
                new CodeProperty
                {
                    Name = "age",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "int"
                    }
                },
                new CodeProperty
                {
                    Name = "height",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "double"
                    }
                },
                new CodeProperty
                {
                    Name = "height2",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "decimal"
                    }
                },
                new CodeProperty
                {
                    Name = "story",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "StreamInterface"
                    }
                },
                new CodeProperty
                {
                    Name = "likes",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "number"
                    }
                },
                new CodeProperty
                {
                    Name = "custom",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType
                    {
                        Name = "Custom"
                    }
                }
            );
            var deserializerMethod = new CodeMethod()
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
            currentClass.AddMethod(deserializerMethod);
            
            _codeMethodWriter.WriteCodeElement(deserializerMethod, writer);
            var result = tw.ToString();

            Assert.Contains("'name' => function (self $o, ParseNode $n) { $o->setName($n->getStringValue()); },", result);
            Assert.Contains("'story' => function (self $o, ParseNode $n) { $o->setStory($n->getBinaryContent()); }", result);
            Assert.Contains(
                "'years' => function (self $o, ParseNode $n) { $o->setYears($n->getCollectionOfPrimitiveValues())",
                result);
            Assert.Contains(
                "'users' => function (self $o, ParseNode $n) { $o->setUsers($n->getCollectionOfObjectValues(EmailAddress::class));",
                result);
        }

        [Fact]
        public void WriteDeserializerMergeWhenHasParent()
        {
            var currentClass = parentClass;
            currentClass.Kind = CodeClassKind.Model;
            var declaration = currentClass.StartBlock as ClassDeclaration;
            declaration.Inherits = new CodeType() {Name = "Entity", IsExternal = true, IsNullable = false};
            currentClass.AddProperty(
                new CodeProperty()
                {
                    Name = "name",
                    Access = AccessModifier.Private,
                    Kind = CodePropertyKind.Custom,
                    Type = new CodeType() {Name = "string"}
                }
            );
            var deserializerMethod = new CodeMethod()
            {
                Name = "getDeserializationFields",
                Kind = CodeMethodKind.Deserializer,
                Description = "Just some random method",
                ReturnType = new CodeType()
                {
                    IsNullable = false,
                    CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                    Name = "array"
                }
            };
            currentClass.AddMethod(deserializerMethod);
            
            _codeMethodWriter.WriteCodeElement(deserializerMethod, writer);
            var result = tw.ToString();

            Assert.Contains("array_merge(parent::getFieldDeserializers()", result);
        }

        [Fact]
        public void WriteConstructorBody()
        {
            var constructor = new CodeMethod()
            {
                Name = "constructor",
                Access = AccessModifier.Public,
                Description = "The constructor for this class",
                ReturnType = new CodeType() {Name = "void"},
                Kind = CodeMethodKind.Constructor
            };
            var closingClass = parentClass;
            parentClass.AddMethod(constructor);
            
            _codeMethodWriter.WriteCodeElement(constructor, writer);
            var result = tw.ToString();

            Assert.Contains("public function __construct", result);
        }

        [Fact]
        public void WriteGetter()
        {
            var getter = new CodeMethod()
            {
                Name = "getEmailAddress",
                Description = "This method gets the emailAddress",
                ReturnType = new CodeType()
                {
                    Name = "emailAddress",
                    IsNullable = false
                },
                Kind = CodeMethodKind.Getter,
                AccessedProperty = new CodeProperty() {Name = "emailAddress", Access = AccessModifier.Private, Type = new CodeType()
                {
                    Name = "emailAddress"
                }},
                Parent = parentClass
            };

            _codeMethodWriter.WriteCodeElement(getter, writer);
            var result = tw.ToString();
            Assert.Contains(": EmailAddress {", result);
            Assert.Contains("public function getEmailAddress", result);
        }

        [Fact]
        public void WriteSetter()
        {
            var setter = new CodeMethod()
            {
                Name = "setEmailAddress",
                ReturnType = new CodeType()
                {
                    Name = "void"
                },
                Kind = CodeMethodKind.Setter,
                AccessedProperty = new CodeProperty() {Name = "emailAddress", Access = AccessModifier.Private, Type = new CodeType()
                {
                    Name = "emailAddress"
                }},
                Parent = parentClass

            };
            
            setter.AddParameter(new CodeParameter()
            {
                Name = "value",
                Kind = CodeParameterKind.SetterValue,
                Type = new CodeType()
                {
                    Name = "emailAddress"
                }
            });
            _codeMethodWriter.WriteCodeElement(setter, writer);
            var result = tw.ToString();

            Assert.Contains("public function setEmailAddress(EmailAddress $value)", result);
            Assert.Contains(": void {", result);
            Assert.Contains("$this->emailAddress = $value", result);
        }

        [Fact]
        public void WriteRequestBuilderWithParametersBody()
        {
            var codeMethod = new CodeMethod()
            {
                ReturnType = new CodeType()
                {
                    Name = "MessageRequestBuilder",
                    IsNullable = false
                },
                Name = "message",
                Parent = parentClass,
                Kind = CodeMethodKind.RequestBuilderWithParameters
            };
            
            codeMethod.AddParameter(new CodeParameter()
            {
                Kind = CodeParameterKind.PathParameters,
                Name = "someParameter",
                Type = new CodeType()
                {
                    Name = "array"
                },
                UrlTemplateParameterName = "rawUrl"
            });
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();
            Assert.Contains("function message(array $pathParameters): MessageRequestBuilder {", result);
            Assert.Contains("return new MessageRequestBuilder($this->pathParameters, $this->requestAdapter);", result);
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
            method.AddParameter(new CodeParameter()
            {
                Name = "requestAdapter",
                Kind = CodeParameterKind.RequestAdapter,
                Type = new CodeType()
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
            
            method.AddParameter(new CodeParameter()
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

            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("__construct", result);
            Assert.Contains($"$this->{propName} = {defaultValue};", result);
            Assert.Contains("$this->pathParameters = array_merge($this->pathParameters, $urlTplParams);", result);
        }
        
        [Fact]
        public void WriteFactoryMethod()
        {
            var codeMethod = new CodeMethod()
            {
                ReturnType = new CodeType()
                {
                    Name = "ParentClass",
                    IsNullable = false
                },
                Name = "createFactory",
                Parent = parentClass,
                Kind = CodeMethodKind.Factory
            };
            
            codeMethod.AddParameter(new CodeParameter()
            {
                Kind = CodeParameterKind.ParseNode,
                Name = "parseNode",
                Type = new CodeType()
                {
                    Name = "ParseNode"
                },
                UrlTemplateParameterName = "rawUrl"
            });
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();
            Assert.Contains("return new ParentClass()", result);
            Assert.Contains(": ParentClass", result);
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
            var codeMethod = new CodeMethod()
            {
                ReturnType = new CodeType()
                {
                    Name = "void",
                    IsNullable = false
                },
                Name = "construct",
                Parent = parentClass,
                Kind = CodeMethodKind.ClientConstructor
            };

            codeMethod.AddParameter(new CodeParameter()
            {
                Kind = CodeParameterKind.RequestAdapter,
                Name = "requestAdapter",
                Type = new CodeType()
                {
                    Name = "RequestAdapter"
                },
                UrlTemplateParameterName = "rawUrl"
            });
            
            _codeMethodWriter.WriteCodeElement(codeMethod, writer);
            var result = tw.ToString();
            Assert.Contains("$this->requestAdapter = $requestAdapter", result);
            Assert.Contains("public function __construct(RequestAdapter $requestAdapter)", result);
        }
    }
}
