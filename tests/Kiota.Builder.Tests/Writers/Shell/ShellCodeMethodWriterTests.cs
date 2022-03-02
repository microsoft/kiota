using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Shell;

public class ShellCodeMethodWriterTests : IDisposable
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
    private const string MethodDescription = "some description";
    private const string ParamDescription = "some parameter description";
    private const string ParamName = "paramName";

    public ShellCodeMethodWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Shell, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
        method = new CodeMethod
        {
            Name = MethodName
        };
        method.ReturnType = new CodeType
        {
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
            Name = "RequestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
        });
        parentClass.AddProperty(new CodeProperty {
            Name = "urlTemplate",
            Kind = CodePropertyKind.UrlTemplate,
        });
    }

    private static void AddRequestBodyParameters(CodeMethod method) {
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
        method.AddParameter(new CodeParameter {
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

    private static void AddPathAndQueryParameters(CodeMethod method) {
        var stringType = new CodeType {
            Name = "string",
        };
        method.AddPathOrQueryParameter(new CodeParameter{
            Name = "q",
            Kind = CodeParameterKind.QueryParameter,
            Type = stringType,
            DefaultValue = "test",
            Description = "The q option",
            Optional = true
        });
        method.AddPathOrQueryParameter(new CodeParameter {
            Name = "p",
            Kind = CodeParameterKind.Path,
            Type = stringType
        });
    }

    [Fact]
    public void WritesRootCommand()
    {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.OriginalMethod = new CodeMethod
        {
            Kind = CodeMethodKind.ClientConstructor
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
            Kind = CodeMethodKind.ClientConstructor
        };
        parentClass.AddMethod(new CodeMethod {
            Name = "BuildUserCommand",
            Kind = CodeMethodKind.CommandBuilder
        });

        writer.Write(method);

        var result = tw.ToString();

        Assert.Contains("var command = new RootCommand();", result);
        Assert.Contains("command.AddCommand(BuildUserCommand());", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesIndexerCommands() {
        method.Kind = CodeMethodKind.CommandBuilder;
        var type = new CodeClass { Name = "TestClass", Kind = CodeClassKind.RequestBuilder };
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", ReturnType = new CodeType {CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array} });
        method.OriginalIndexer = new CodeIndexer {
            ReturnType = new CodeType {
                Name = "TestRequestBuilder",
                TypeDefinition = type
            }
        };

        AddRequestProperties();

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var builder = new TestRequestBuilder", result);
        Assert.Contains("var commands = new List<Command>();", result);
        Assert.Contains("commands.Add(builder.BuildTestMethod1());", result);
        Assert.Contains("commands.AddRange(builder.BuildTestMethod2());", result);
        Assert.Contains("return commands;", result);
    }

    [Fact]
    public void WritesContainerCommands() {
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        var type = new CodeClass { Name = "TestClass", Kind = CodeClassKind.RequestBuilder };
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod1", ReturnType = new CodeType() });
        type.AddMethod(new CodeMethod { Kind = CodeMethodKind.CommandBuilder, Name = "BuildTestMethod2", ReturnType = new CodeType {CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array} });
        type.Parent = new CodeType {
            Name = "Test.Name"
        };
        method.AccessedProperty = new CodeProperty {
            Type = new CodeType {
                Name = "TestRequestBuilder",
                TypeDefinition = type
            }
        };

        AddRequestProperties();

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var builder = new Test.Name.TestRequestBuilder", result);
        Assert.Contains("command.AddCommand(builder.BuildTestMethod1());", result);
        Assert.Contains("foreach (var cmd in builder.BuildTestMethod2()) {", result);
        Assert.Contains("command.AddCommand(cmd);", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForGetRequest() {
        
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Get;
        var stringType = new CodeType {
            Name = "string",
        };
        var generatorMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreateGetRequestInformation",
            HttpMethod = method.HttpMethod
        };
        method.OriginalMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddRequestBodyParameters(method.OriginalMethod);
        AddPathAndQueryParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("command.AddOption(outputOption);", result);
        Assert.Contains("command.SetHandler(async (object[] parameters) => {", result);
        Assert.Contains("var q = (string) parameters[0];", result);
        Assert.Contains("PathParameters.Clear();", result);
        Assert.Contains("PathParameters.Add(\"p\", p);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("var response = await RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("}, new CollectionBinding(qOption,", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostRequest() {
        
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType {
            Name = "string",
        };
        var generatorMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            HttpMethod = method.HttpMethod
        };
        method.OriginalMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = stringType,
            Parent = method.Parent
        };
        method.OriginalMethod.AddParameter(new CodeParameter{
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = stringType,
        });
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathAndQueryParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var bodyOption = new Option<string>(\"--body\")", result);
        Assert.Contains("bodyOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(bodyOption);", result);
        Assert.Contains("command.AddOption(outputOption);", result);
        Assert.Contains("PathParameters.Clear();", result);
        Assert.Contains("PathParameters.Add(\"p\", p);", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("var response = await RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("return command;", result);
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
            HttpMethod = method.HttpMethod
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
        AddPathAndQueryParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("command.AddOption(outputOption);", result);
        Assert.Contains("var fileOption = new Option<FileInfo>(\"--file\");", result);
        Assert.Contains("command.AddOption(fileOption);", result);
        Assert.Contains("command.SetHandler(async (object[] parameters) => {", result);
        Assert.Contains("var q = (string) parameters[0];", result);
        Assert.Contains("PathParameters.Clear();", result);
        Assert.Contains("PathParameters.Add(\"p\", p);", result);
        Assert.Contains("var requestInfo = CreateGetRequestInformation", result);
        Assert.Contains("var response = await RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("}, new CollectionBinding(qOption,", result);
        Assert.Contains("return command;", result);
    }

    [Fact]
    public void WritesExecutableCommandForPostVoidRequest() {
        
        method.Kind = CodeMethodKind.CommandBuilder;
        method.SimpleName = "User";
        method.HttpMethod = HttpMethod.Post;
        var stringType = new CodeType {
            Name = "string",
        };
        var voidType = new CodeType {
            Name = "void",
        };
        var generatorMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestGenerator,
            Name = "CreatePostRequestInformation",
            HttpMethod = method.HttpMethod
        };
        method.OriginalMethod = new CodeMethod {
            Kind = CodeMethodKind.RequestExecutor,
            HttpMethod = method.HttpMethod,
            ReturnType = voidType,
            Parent = method.Parent
        };
        method.OriginalMethod.AddParameter(new CodeParameter{
            Name = "body",
            Kind = CodeParameterKind.RequestBody,
            Type = stringType,
        });
        var codeClass = method.Parent as CodeClass;
        codeClass.AddMethod(generatorMethod);

        AddRequestProperties();
        AddPathAndQueryParameters(generatorMethod);

        writer.Write(method);
        var result = tw.ToString();

        Assert.Contains("var command = new Command(\"user\");", result);
        Assert.Contains("var qOption = new Option<string>(\"-q\", getDefaultValue: ()=> \"test\", description: \"The q option\")", result);
        Assert.Contains("qOption.IsRequired = false;", result);
        Assert.Contains("command.AddOption(qOption);", result);
        Assert.Contains("var bodyOption = new Option<string>(\"--body\")", result);
        Assert.Contains("bodyOption.IsRequired = true;", result);
        Assert.Contains("command.AddOption(bodyOption);", result);
        Assert.Contains("PathParameters.Clear();", result);
        Assert.Contains("PathParameters.Add(\"p\", p);", result);
        Assert.Contains("var requestInfo = CreatePostRequestInformation", result);
        Assert.Contains("await RequestAdapter.SendNoContentAsync(requestInfo, errorMapping: default, cancellationToken: cancellationToken);", result);
        Assert.Contains("Console.WriteLine(\"Success\");", result);
        Assert.Contains("return command;", result);
    }
}
