using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Cli;

public class CliCodeMethodWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeMethod method;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace root;
    private const string MethodName = "methodName";
    private const string ReturnTypeName = "Somecustomtype";

    public CliCodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CLI, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        root.Name = "Test";
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        method = new CodeMethod
        {
            Name = MethodName,
            ReturnType = new CodeType
            {
                Name = ReturnTypeName
            }
        };
        parentClass.AddMethod(method);
    }

    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void AddRequestProperties()
    {
        parentClass.AddProperty(new CodeProperty
        {
            Name = "reqAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "reqAdapter",
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "PathParameters",
            },
        });
        parentClass.AddProperty(new CodeProperty
        {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
            Type = new CodeType
            {
                Name = "string",
            },
        });
    }

    private static void AddRequestBodyParameters(CodeMethod method)
    {
        var stringType = new CodeType
        {
            Name = "string",
        };
        method.AddParameter(new CodeParameter
        {
            Name = "h",
            Kind = CodeParameterKind.Headers,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "q",
            Kind = CodeParameterKind.QueryParameter,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "b",
            Kind = CodeParameterKind.RequestBody,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "o",
            Kind = CodeParameterKind.Options,
            Type = stringType,
        });
        method.AddParameter(new CodeParameter
        {
            Name = "c",
            Kind = CodeParameterKind.Cancellation,
            Type = stringType,
        });
    }

    private static void AddPathQueryAndHeaderParameters(CodeMethod method)
    {
        var stringType = new CodeType
        {
            Name = "string",
        };
        method.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "q",
            Kind = CodeParameterKind.QueryParameter,
            Type = stringType,
            DefaultValue = "test",
            Documentation = new()
            {
                Description = "The q option",
            },
            Optional = true
        });
        method.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "test-path",
            Kind = CodeParameterKind.Path,
            Type = stringType
        });
        method.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "Test-Header",
            Kind = CodeParameterKind.Headers,
            Type = stringType,
            Documentation = new()
            {
                Description = "The test header",
            },
        });
    }

    [Fact]
    public void WritesRootCommand()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType
            {
                Name = "RootCommand",
            }
        };

        writer.Write(method);

        var result = tw.ToString();

        Assert.Contains("var command = new RootCommand();", result);

        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesRootCommandWithCommandBuilderMethods()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.ClientConstructor,
            ReturnType = new CodeType
            {
                Name = "RootCommand",
            }
        };
        parentClass.AddMethod(new CodeMethod
        {
            Name = "BuildUserCommand",
            Kind = CodeMethodKind.CommandBuilder,
            ReturnType = new CodeType
            {
                Name = "Command",
            }
        });

        writer.Write(method);

        var result = tw.ToString();

        Assert.Contains("var command = new RootCommand();", result);
        Assert.Contains("command.AddCommand(BuildUserCommand());", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesIndexerCommands()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        var type = new CodeClass { Name = "TestClass", Kind = CodeClassKind.RequestBuilder };
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", ReturnType = new CodeType { CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array } });
        type.Parent = CodeNamespace.InitRootNamespace();
        type.Parent.Name = "Test.Name.Sub";
        method.OriginalIndexer = new CodeIndexer
        {
            ReturnType = new CodeType
            {
                Name = "TestItemRequestBuilder",
                TypeDefinition = type
            },
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string",
                },
                SerializationName = "test",
            }
        };

        AddRequestProperties();

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var builder = new TestItemRequestBuilder", result);
        Assert.Contains("var commands = new List<Command>();", result);
        Assert.Contains("commands.Add(builder.BuildTestMethod1());", result);
        Assert.Contains("commands.AddRange(builder.BuildTestMethod2());", result);
        Assert.Contains("return new(new(0), commands);", result);
    }

    [Fact]
    public void WritesMatchingIndexerCommandsIntoExecutableCommand()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        var type = new CodeClass { Name = "TestItemRequestBuilder", Kind = CodeClassKind.RequestBuilder };
        var tupleReturn = new CodeType
        {
            Name = "Tuple",
            IsExternal = true,
            GenericTypeParameterValues = new() {
                new CodeType { CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array },
                new CodeType { CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array },
            },
        };
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", SimpleName = "User", ReturnType = new CodeType() });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", SimpleName = "User", ReturnType = tupleReturn });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod3", SimpleName = "Test", ReturnType = new CodeType() });
        type.Parent = CodeNamespace.InitRootNamespace();
        type.Parent.Name = "Test.Name.Sub";
        var indexerReturn = new CodeType
        {
            Name = "TestItemRequestBuilder",
            TypeDefinition = type
        };
        var im = new CodeMethod
        {
            ReturnType = new CodeType
            {
                Name = "Command",
                IsExternal = true
            },
            Kind = CodeMethodKind.CommandBuilder,
            SimpleName = "testItem-indexer",
            OriginalIndexer = new CodeIndexer
            {
                ReturnType = indexerReturn,
                Name = "testItem-idx",
                IndexParameter = new()
                {
                    Name = "id",
                    Type = new CodeType
                    {
                        Name = "string",
                    },
                }
            }
        };

        codeClass.AddMethod(im);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var testItemIdx = new TestItemRequestBuilder();", result);
        Assert.Contains("var command = testItemIdx.BuildTestMethod1();", result);
        Assert.Contains("var cmds = testItemIdx.BuildTestMethod2();", result);
        Assert.DoesNotContain("execCommands.AddRange(cmds.Item1)", result);
        Assert.Contains("nonExecCommands.AddRange(cmds.Item2)", result);
        Assert.Contains("command.AddCommand(cmd);", result);
        Assert.Contains("command.SetHandler(async (invocationContext)", result);
        Assert.Contains("return command;", result);
        Assert.DoesNotContain("command.AddCommand(builder.BuildTestMethod3());", result);
        var lines = result.Split('\n');
        Assert.Equal(0, lines.Count(l => l.Contains("var execCommands = new List<Command>()")));
        Assert.Equal(1, lines.Count(l => l.Contains("var nonExecCommands = new List<Command>()")));
        Assert.Equal(0, lines.Count(l => l.Contains("foreach (var cmd in execCommands)")));
        Assert.Equal(1, lines.Count(l => l.Contains("foreach (var cmd in nonExecCommands.OrderBy(static c => c.Name, StringComparer.Ordinal))")));
    }

    [Fact]
    public void WritesMatchingIndexerCommandsIntoContainerCommand()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";

        var ns = CodeNamespace.InitRootNamespace();
        ns.Name = "Test.Name.Sub";
        var codeClass = method.Parent as CodeClass;
        var navTd = new CodeClass
        {
            Name = "TestNavItemRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
            Parent = codeClass.Parent
        };
        method.AccessedProperty = new CodeProperty
        {
            Name = "TestProperty",
            Type = new CodeType
            {
                Name = "TestRequestBuilder",
                TypeDefinition = navTd
            }
        };

        navTd.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod11", SimpleName = "Test", ReturnType = new CodeType() });

        var type = new CodeClass { Name = "TestIndexItemRequestBuilder", Kind = CodeClassKind.RequestBuilder };

        var tupleReturn = new CodeType
        {
            Name = "Tuple",
            IsExternal = true,
            GenericTypeParameterValues = new() {
                new CodeType { CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array },
                new CodeType { CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array },
            },
        };
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", SimpleName = "User", ReturnType = new CodeType() });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", SimpleName = "User", ReturnType = tupleReturn });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod3", SimpleName = "Test", ReturnType = new CodeType() });
        type.Parent = ns;
        var indexerReturn = new CodeType
        {
            Name = "TestIndexItemRequestBuilder",
            TypeDefinition = type
        };
        var im = new CodeMethod
        {
            Name = "TestItem",
            ReturnType = new CodeType
            {
                Name = "Command",
                IsExternal = true
            },
            Kind = CodeMethodKind.CommandBuilder,
            SimpleName = "testItem-indexer",
            OriginalIndexer = new CodeIndexer
            {
                ReturnType = indexerReturn,
                Name = "testItem-indexer",
                IndexParameter = new()
                {
                    Name = "id",
                    Type = new CodeType
                    {
                        Name = "string",
                    },
                }
            }
        };

        codeClass.AddMethod(im);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var testItemIndexer = new TestIndexItemRequestBuilder();", result);
        Assert.Contains("var command = testItemIndexer.BuildTestMethod1();", result);
        Assert.Contains("var cmds = testItemIndexer.BuildTestMethod2();", result);
        Assert.DoesNotContain("execCommands.AddRange(cmds.Item1);", result);
        Assert.Contains("nonExecCommands.AddRange(cmds.Item2);", result);
        Assert.Contains("var builder = new TestRequestBuilder", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod11());", result);
        Assert.Contains("return command;", result);
        Assert.DoesNotContain("nonExecCommands.Add(builder.BuildTestMethod3());", result);
        var lines = result.Split('\n');
        Assert.Equal(0, lines.Count(l => l.Contains("var execCommands = new List<Command>()")));
        Assert.Equal(1, lines.Count(l => l.Contains("var nonExecCommands = new List<Command>()")));
        Assert.Equal(0, lines.Count(l => l.Contains("foreach (var cmd in execCommands)")));
        Assert.Equal(1, lines.Count(l => l.Contains("foreach (var cmd in nonExecCommands.OrderBy(static c => c.Name, StringComparer.Ordinal))")));
    }

    [Fact]
    public void WritesExecutableCommandThatReusesMatchingNavCommandInstance()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var stringType = new CodeType { Name = "string" };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);
        var im = new CodeMethod
        {
            Name = "BuildTestItemNavCommand",
            ReturnType = new CodeType
            {
                Name = "Command",
                IsExternal = true
            },
            Kind = CodeMethodKind.CommandBuilder,
            SimpleName = "User",
            AccessedProperty = new CodeProperty
            {
                Name = "TestProperty",
                Type = new CodeType
                {
                    Name = "TestRequestBuilder",
                    TypeDefinition = new CodeClass
                    {
                        Name = "TestNavItemRequestBuilder",
                        Kind = CodeClassKind.RequestBuilder,
                        Parent = codeClass.Parent
                    }
                }
            }
        };
        codeClass.AddMethod(im);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = BuildTestItemNavCommand();", result);
        Assert.Contains("command.Description = \"Test description\";", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesNavCommandThatSkipsReusedNavCommandInstance()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";

        var ns = CodeNamespace.InitRootNamespace();
        ns.Name = "Test.Name.Sub";
        var codeClass = method.Parent as CodeClass;
        var navTd = new CodeClass
        {
            Name = "TestNavItemRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
            Parent = codeClass.Parent
        };
        method.AccessedProperty = new CodeProperty
        {
            Name = "TestProperty",
            Type = new CodeType
            {
                Name = "TestNavRequestBuilder",
                TypeDefinition = navTd
            }
        };

        navTd.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.CommandBuilder,
            Name = "BuildNavTestMethod",
            SimpleName = "Test",
            ReturnType = new CodeType(),
            AccessedProperty = new CodeProperty
            {
                Name = "TestSubProperty1",
                Type = new CodeType
                {
                    Name = "TestSubRequestBuilder"
                }
            }
        });
        navTd.AddMethod(new CodeMethod
        {
            Kind = CodeMethodKind.CommandBuilder,
            Name = "BuildExecutableTestMethod",
            SimpleName = "Test",
            ReturnType = new CodeType(),
            OriginalMethod = new CodeMethod
            {
                Kind = CodeMethodKind.RequestExecutor,
                HttpMethod = HttpMethod.Post,
                ReturnType = new CodeType(),
                Parent = method.Parent
            }
        });

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var builder = new TestNavRequestBuilder();", result);
        Assert.Contains("execCommands.Add(builder.BuildExecutableTestMethod());", result);
        Assert.Contains("return command;", result);
        Assert.DoesNotContain("BuildNavTestMethod", result);
        var lines = result.Split('\n');
        Assert.Equal(1, lines.Count(l => l.Contains("var execCommands = new List<Command>()")));
        Assert.Equal(0, lines.Count(l => l.Contains("var nonExecCommands = new List<Command>()")));
        Assert.Equal(1, lines.Count(l => l.Contains("foreach (var cmd in execCommands)")));
        Assert.Equal(0, lines.Count(l => l.Contains("foreach (var cmd in nonExecCommands)")));
    }

    [Fact]
    public void WritesContainerCommands()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        // Types: A.B.C.T2
        //        A.B.C.D.T1
        var ns1 = root.AddNamespace("Test.Name");
        var ns2 = ns1.AddNamespace("Test.Name.Sub1");
        var ns3 = ns2.AddNamespace("Test.Name.Sub1.Sub2");
        var t2 = parentClass;
        ns2.AddClass(t2);
        var t1Sub = new CodeClass { Name = "TestClass1", Kind = CodeClassKind.RequestBuilder };
        ns3.AddClass(t1Sub);

        t1Sub.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });
        t1Sub.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", ReturnType = new CodeType() });
        method.AccessedProperty = new CodeProperty
        {
            Type = new CodeType
            {
                Name = "TestNavRequestBuilder",
                TypeDefinition = t1Sub
            }
        };

        AddRequestProperties();

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var builder = new TestNavRequestBuilder", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod1());", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod2());", result);
        Assert.Contains("return command;", result);
        var lines = result.Split('\n');
        Assert.Equal(0, lines.Count(l => l.Contains("var execCommands = new List<Command>()")));
        Assert.Equal(1, lines.Count(l => l.Contains("var nonExecCommands = new List<Command>()")));
        Assert.Equal(0, lines.Count(l => l.Contains("foreach (var cmd in execCommands)")));
        Assert.Equal(1, lines.Count(l => l.Contains("foreach (var cmd in nonExecCommands)")));
    }

    [Fact]
    public void WritesRequestBuilderWithParametersCommands()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        var ns1 = root.AddNamespace("Test.Name");
        var ns2 = ns1.AddNamespace("Test.Name.Sub1");
        var ns3 = ns2.AddNamespace("Test.Name.Sub1.Sub2");
        var t2 = parentClass;
        ns2.AddClass(t2);
        var t1Sub = new CodeClass { Name = "TestClass1", Kind = CodeClassKind.RequestBuilder };
        ns3.AddClass(t1Sub);

        t1Sub.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });
        t1Sub.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", ReturnType = new CodeType() });
        method.OriginalMethod = new CodeMethod
        {
            ReturnType = new CodeType
            {
                Name = "TestRequestBuilderWithA",
                TypeDefinition = t1Sub
            }
        };

        AddRequestProperties();

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var builder = new TestRequestBuilderWithA", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod1());", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod2());", result);
        Assert.Contains("return command;", result);
        var lines = result.Split('\n');
        Assert.Equal(0, lines.Count(l => l.Contains("var execCommands = new List<Command>()")));
        Assert.Equal(1, lines.Count(l => l.Contains("var nonExecCommands = new List<Command>()")));
        Assert.Equal(0, lines.Count(l => l.Contains("foreach (var cmd in execCommands)")));
        Assert.Equal(1, lines.Count(l => l.Contains("foreach (var cmd in nonExecCommands)")));
    }

    [Fact]
    public void WritesContainerCommandWithConflictingTypes()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        // Types: A.B.T1
        //        A.B.C.D.T2
        //        A.B.C.D.E.F.T1
        var ns1 = root.AddNamespace("A.B");
        var ns2 = ns1.AddNamespace("A.B.C.D");
        var ns3 = ns2.AddNamespace("A.B.C.D.E.F");
        parentClass.Kind = CodeClassKind.RequestBuilder;
        var t1 = new CodeClass { Name = "TestRequestBuilder", Kind = CodeClassKind.RequestBuilder };
        var t1a = new CodeClass { Name = "TestRequestBuilder2", Kind = CodeClassKind.RequestBuilder };
        ns1.AddClass(t1);
        ns1.AddClass(t1a);
        var t2 = parentClass;
        ns2.AddClass(t2);
        var t1Sub = new CodeClass { Name = "TestRequestBuilder", Kind = CodeClassKind.RequestBuilder };
        var t1Sub2 = new CodeClass { Name = "testRequestBuilder2", Kind = CodeClassKind.RequestBuilder }; // Should match ignoring case
        ns3.AddClass(t1Sub);
        ns3.AddClass(t1Sub2);

        t1Sub.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });

        method.AccessedProperty = new CodeProperty
        {
            Type = new CodeType
            {
                Name = "TestRequestBuilder",
                TypeDefinition = t1Sub
            }
        };
        var method2 = new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "methodName2", SimpleName = "Mail", ReturnType = new CodeType { Name = ReturnTypeName } };
        parentClass.AddMethod(method2);

        t1Sub2.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });

        method2.AccessedProperty = new CodeProperty
        {
            Type = new CodeType
            {
                Name = "testRequestBuilder2",
                TypeDefinition = t1Sub2
            }
        };

        AddRequestProperties();

        writer.Write(method);
        writer.Write(method2);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var builder = new Test.A.B.C.D.E.F.TestRequestBuilder", result);
        // Test case insensitive match
        Assert.Contains("var builder = new Test.A.B.C.D.E.F.TestRequestBuilder2", result);
        Assert.Contains("nonExecCommands.Add(builder.BuildTestMethod1());", result);
        Assert.Contains("foreach (var cmd in nonExecCommands)", result);
        Assert.Contains("command.AddCommand(cmd);", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandWithRelatedLinksInDescription()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.Documentation.DocumentationLink = new Uri("https://test.com/help/description");
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathQueryAndHeaderParameters(generatorMethod);
        generatorMethod.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "testDoc",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true,
            },
            Documentation = new()
            {
                DocumentationLink = new Uri("https://test.com/help/description")
            }
        });
        generatorMethod.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "testDoc2",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true,
            },
            Documentation = new()
            {
                Description = "Documentation label2",
                DocumentationLink = new Uri("https://test.com/help/description")
            }
        });
        generatorMethod.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "testDoc3",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "string",
                IsNullable = true,
            },
            Documentation = new()
            {
                Description = "Documentation label3",
                DocumentationLabel = "Test label",
                DocumentationLink = new Uri("https://test.com/help/description")
            }
        });

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("command.Description = \"Test description\\n\\nRelated Links:\\n  https://test.com/help/description\";", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Matches("var testHeaderOption = new Option<string\\[]>\\(\"--test-header\", description: \"The test header\"\\) {\\s+Arity = ArgumentArity.OneOrMore", result);
        Assert.Contains("testHeaderOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(testHeaderOption);", result);
        // Should generated code have Option<string?> instead? Currently for the CLI, it doesn't matter since GetValueForOption always returns nullable types
        Assert.Contains("var testDocOption = new Option<string>(\"--test-doc\", description: \"See: https://test.com/help/description\")", result);
        Assert.Contains("var testDoc2Option = new Option<string>(\"--test-doc2\", description: \"Documentation label2\\nSee: https://test.com/help/description\")", result);
        Assert.Contains("var testDoc3Option = new Option<string>(\"--test-doc3\", description: \"Documentation label3\\nTest label: https://test.com/help/description\")", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("var q = invocationContext.ParseResult.GetValueForOption(qOption);", result);
        Assert.Contains("var testHeader = invocationContext.ParseResult.GetValueForOption(testHeaderOption);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("if (testHeader is not null) requestInfo.Headers.Add(\"Test-Header\", testHeader);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("IOutputFormatterFactory outputFormatterFactory = invocationContext.BindingContext.GetService(typeof(IOutputFormatterFactory)) as IOutputFormatterFactory ?? throw new ArgumentNullException(\"outputFormatterFactory\");", result);
        Assert.Contains("var formatter = outputFormatterFactory.GetFormatter(FormatterType.TEXT);", result);
        Assert.Contains("await formatter.WriteOutputAsync(response, null, cancellationToken);", result);
        Assert.Contains("});", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForGetRequestPrimitive()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathQueryAndHeaderParameters(generatorMethod);
        generatorMethod.AddPathQueryOrHeaderParameter(new CodeParameter
        {
            Name = "count",
            Kind = CodeParameterKind.QueryParameter,
            Type = new CodeType
            {
                Name = "boolean",
                IsNullable = true,
            },
        });

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("command.Description = \"Test description\";", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Matches("var testHeaderOption = new Option<string\\[]>\\(\"--test-header\", description: \"The test header\"\\) {\\s+Arity = ArgumentArity.OneOrMore", result);
        Assert.Contains("testHeaderOption.IsRequired = true;", result);
        Assert.Contains("var countOption = new Option<bool?>(\"--count\")", result);
        Assert.Contains("command.AddOption(testHeaderOption);", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("var q = invocationContext.ParseResult.GetValueForOption(qOption);", result);
        Assert.Contains("var testHeader = invocationContext.ParseResult.GetValueForOption(testHeaderOption);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("if (testHeader is not null) requestInfo.Headers.Add(\"Test-Header\", testHeader);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("IOutputFormatterFactory outputFormatterFactory = invocationContext.BindingContext.GetService(typeof(IOutputFormatterFactory)) as IOutputFormatterFactory ?? throw new ArgumentNullException(\"outputFormatterFactory\");", result);
        Assert.Contains("var formatter = outputFormatterFactory.GetFormatter(FormatterType.TEXT);", result);
        Assert.Contains("await formatter.WriteOutputAsync(response, null, cancellationToken);", result);
        Assert.Contains("});", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPagedGetRequestModel()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var userClass = root.AddClass(new CodeClass
        {
            Name = "User",
            Kind = CodeClassKind.Model
        }).First();
        var stringType = new CodeType
        {
            Name = "user",
            TypeDefinition = userClass,
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent,
            PagingInformation = new()
            {
                NextLinkName = "nextLink",
                ItemName = "item"
            },
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("command.Description = \"Test description\";", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("var jsonNoIndentOption = new Option<bool>(\"--json-no-indent\", r => {", result);
        Assert.Contains("var allOption = new Option<bool>(\"--all\")", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("command.AddOption(jsonNoIndentOption);", result);
        Assert.Contains("command.AddOption(outputOption);", result);
        Assert.Contains("command.AddOption(allOption);", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("var q = invocationContext.ParseResult.GetValueForOption(qOption);", result);
        Assert.Contains("var all = invocationContext.ParseResult.GetValueForOption(allOption)", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var pagingData = new PageLinkData(requestInfo, null, itemName: \"item\", nextLinkName: \"nextLink\");", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var pageResponse = await pagingService.GetPagedDataAsync((info, token) => reqAdapter.SendNoContentAsync(info, cancellationToken: token), pagingData, all, cancellationToken);", result);
        Assert.Contains("formatterOptions = output.GetOutputFormatterOptions(new FormatterOptionsModel(!jsonNoIndent));", result);
        Assert.Contains("IOutputFormatter? formatter = null;", result);
        Assert.Contains("if (pageResponse?.StatusCode >= 200 && pageResponse?.StatusCode < 300) {", result);
        Assert.Contains("formatter = outputFormatterFactory.GetFormatter(output);", result);
        Assert.Contains("response = (response != Stream.Null) ? await outputFilter.FilterOutputAsync(response, query, cancellationToken) : response", result);
        Assert.Contains("formatter = outputFormatterFactory.GetFormatter(FormatterType.TEXT);", result);
        Assert.Contains("await formatter.WriteOutputAsync(response, formatterOptions, cancellationToken);", result);
        Assert.Contains("});", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForGetRequestModel()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.Documentation.Description = "Test description";
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var userClass = root.AddClass(new CodeClass
        {
            Name = "User",
            Kind = CodeClassKind.Model
        }).First();
        var stringType = new CodeType
        {
            Name = "user",
            TypeDefinition = userClass,
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("command.Description = \"Test description\";", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("var jsonNoIndentOption = new Option<bool>(\"--json-no-indent\", r => {", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("command.AddOption(jsonNoIndentOption);", result);
        Assert.Contains("command.AddOption(outputOption);", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("var q = invocationContext.ParseResult.GetValueForOption(qOption);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("var formatterOptions = output.GetOutputFormatterOptions(new FormatterOptionsModel(!jsonNoIndent));", result);
        Assert.Contains("response = (response != Stream.Null) ? await outputFilter.FilterOutputAsync(response, query, cancellationToken) : response", result);
        Assert.Contains("await formatter.WriteOutputAsync(response, formatterOptions, cancellationToken);", result);
        Assert.Contains("});", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostRequestWithModelBody()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var contentClass = root.AddClass(new CodeClass
        {
            Name = "Content",
            Kind = CodeClassKind.Model
        }).First();
        var bodyType = new CodeType
        {
            Name = "content",
            TypeDefinition = contentClass,
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            RequestBodyContentType = "application/text",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var bodyParam = new CodeParameter
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = bodyType,
        };
        method.OriginalMethod.AddParameter(bodyParam);
        generatorMethod.AddParameter(bodyParam);
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var bodyOption = new Option<string>(\"--body\")", result);
        Assert.Contains("bodyOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(bodyOption);", result);
        Assert.Contains("var body = invocationContext.ParseResult.GetValueForOption(bodyOption) ?? string.Empty;", result);
        Assert.Contains("using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));", result);
        Assert.Contains("var model = parseNode.GetObjectValue<Content>(Content.CreateFromDiscriminatorValue);", result);
        Assert.Contains("if (model is null) return;", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("requestInfo.SetContentFromParsable(reqAdapter, \"application/text\", model);", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostRequestWithCollectionModel()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var contentClass = root.AddClass(new CodeClass
        {
            Name = "Content",
            Kind = CodeClassKind.Model
        }).First();
        var bodyType = new CodeType
        {
            Name = "content",
            TypeDefinition = contentClass,
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        method.OriginalMethod.AddParameter(new CodeParameter
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = bodyType,
        });
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var bodyOption = new Option<string>(\"--body\")", result);
        Assert.Contains("bodyOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(bodyOption);", result);
        Assert.Contains("var body = invocationContext.ParseResult.GetValueForOption(bodyOption) ?? string.Empty;", result);
        Assert.Contains("using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));", result);
        Assert.Contains("var model = parseNode.GetCollectionOfObjectValues<Content>(Content.CreateFromDiscriminatorValue)?.ToList();", result);
        Assert.Contains("if (model is null) return;", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostRequestWithStreamBody()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var bodyType = new CodeType
        {
            Name = "stream"
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        method.OriginalMethod.AddParameter(new CodeParameter
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = bodyType,
        });
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var inputFileOption = new Option<FileInfo>(\"--input-file\")", result);
        Assert.Contains("inputFileOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(inputFileOption);", result);
        Assert.Contains("var inputFile = invocationContext.ParseResult.GetValueForOption(inputFileOption);", result);
        Assert.Contains("if (inputFile is null || !inputFile.Exists) return;", result);
        Assert.Contains("using var stream = inputFile.OpenRead();", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForDeleteRequest()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Delete;
        var stringType = new CodeType
        {
            Name = "void",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateDeleteRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var requestInfo = CreateDeleteRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("await reqAdapter.SendNoContentAsync(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("Console.WriteLine(\"Success\");", result);
        Assert.Contains("return command;", result);
        Assert.DoesNotContain("command.AddOption(outputOption);", result);
        Assert.DoesNotContain("var jsonNoIndentOption = new Option<bool>(\"--json-no-indent\", r => {", result);
    }

    [Fact]
    public void WritesExecutableCommandForGetStreamRequest()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var streamType = new CodeType
        {
            Name = "stream",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = streamType,
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = streamType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var outputFileOption = new Option<FileInfo>(\"--output-file\");", result);
        Assert.Contains("command.AddOption(outputFileOption);", result);
        Assert.Contains("command.SetHandler(async (invocationContext) => {", result);
        Assert.Contains("var q = invocationContext.ParseResult.GetValueForOption(qOption);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("var response = await reqAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken) ?? Stream.Null;", result);
        Assert.Contains("using var writeStream = outputFile.OpenWrite();", result);
        Assert.Contains("});", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostVoidRequest()
    {

        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType
        {
            Name = "string",
        };
        var voidType = new CodeType
        {
            Name = "void",
        };
        var generatorMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            HttpMethod = method.HttpMethod,
            ReturnType = stringType
        };
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = voidType,
            Parent = method.Parent
        };
        method.OriginalMethod.AddParameter(new CodeParameter
        {
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = stringType,
        });
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathQueryAndHeaderParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var bodyOption = new Option<string>(\"--body\")", result);
        Assert.Contains("bodyOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(bodyOption);", result);
        Assert.Contains("var body = invocationContext.ParseResult.GetValueForOption(bodyOption) ?? string.Empty;", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("if (testPath is not null) requestInfo.PathParameters.Add(\"test%2Dpath\", testPath);", result);
        Assert.Contains("var reqAdapter = invocationContext.GetRequestAdapter()", result);
        Assert.Contains("await reqAdapter.SendNoContentAsync(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("Console.WriteLine(\"Success\");", result);
        Assert.Contains("return command;", result);
        Assert.DoesNotContain("response = (response != Stream.Null) ? await outputFilter.FilterOutputAsync(response, query, cancellationToken) : response", result);
        Assert.DoesNotContain("await formatter.WriteOutputAsync(response, formatterOptions, cancellationToken);", result);
    }
}
