﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Cli;
partial class CliCodeMethodWriter : CodeMethodWriter
{
    private const string AllParamType = "bool";
    private const string AllParamName = "all";
    private const string CancellationTokenParamType = "CancellationToken";
    private const string CancellationTokenParamName = "cancellationToken";
    private const string FileParamType = "FileInfo";
    private const string InputFileParamName = "inputFile";
    private const string OutputFileParamName = "outputFile";
    private const string OutputFilterParamType = "IOutputFilter";
    private const string OutputFilterParamName = "outputFilter";
    private const string OutputFilterQueryParamType = "string";
    private const string OutputFilterQueryParamName = "query";
    private const string OutputFormatParamType = "FormatterType";
    private const string OutputFormatParamName = "output";
    private const string OutputFormatterFactoryParamType = "IOutputFormatterFactory";
    private const string OutputFormatterFactoryParamName = "outputFormatterFactory";
    private const string PagingServiceParamType = "IPagingService";
    private const string PagingServiceParamName = "pagingService";
    private const string RequestAdapterParamType = "IRequestAdapter";
    private const string RequestAdapterParamName = "reqAdapter";
    private const string BuilderInstanceName = "builder";
    private const string InvocationContextParamName = "invocationContext";
    private const string CommandVariableName = "command";
    private const string ExecCommandsVariableName = "execCommands";
    private const string NonExecCommandsVariableName = "nonExecCommands";
    private const string IndexerReturn = "Tuple";

    public CliCodeMethodWriter(CSharpConventionService conventionService) : base(conventionService)
    {
    }

    protected override void WriteCommandBuilderBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        var classMethods = parentClass.Methods;
        var name = codeElement.SimpleName;
        name = UppercaseRegex().Replace(name, "-$1").TrimStart('-').ToLowerInvariant();

        if (codeElement.HttpMethod == null)
        {
            if (codeElement.OriginalMethod is not null && codeElement.OriginalMethod.IsOfKind(CodeMethodKind.ClientConstructor))
            {
                // Assumption is that this is only ever called once.
                WriteRootBuildCommand(codeElement, writer, classMethods);
            }
            else if (codeElement.OriginalIndexer is not null)
            {
                WriteIndexerBuildCommand(codeElement.OriginalIndexer, codeElement, writer, parentClass);
            }
            else
            {
                WriteNavCommand(codeElement, writer, parentClass, name);
            }
        }
        else
        {
            WriteExecutableCommand(codeElement, parentClass, requestParams, writer, name);
        }
    }

    private void WriteExecutableCommand(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, LanguageWriter writer, string name)
    {
        if (parentClass
            .UnorderedMethods
            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod) is not CodeMethod generatorMethod ||
            codeElement.OriginalMethod is not { } originalMethod) return;
        var parametersList = generatorMethod.PathQueryAndHeaderParameters.Where(static p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        if (originalMethod.Parameters.OfKind(CodeParameterKind.RequestBody) is { } bodyParam)
        {
            parametersList.Add(bodyParam);
        }

        if (originalMethod.Parameters.OfKind(CodeParameterKind.RequestBodyContentType) is { } bodyContentTypeParam)
        {
            parametersList.Add(bodyContentTypeParam);
        }

        var (includedSubCommands, builderVarName) = InitializeSharedCommand(codeElement, parentClass, writer, name);
        AddCommands(writer, Enumerable.Empty<CodeMethod>(), includedSubCommands, builderVarName);

        // investigate exploding query params
        // Check the possible formatting options for headers in a cli.
        // -h A=b -h
        // -h A:B,B:C
        // -h {"A": "B"}
        // parameters: (type, name, CodeParameter)
        var parameters = parametersList.Select<CodeParameter, (string typeName, string name, CodeParameter? param)>(p =>
        {
            // Assume headers are a list. Allows users to specify multiple header values.
            // --header <val1> --header <val2>
            if (p.IsOfKind(CodeParameterKind.Headers))
            {
                p.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            }

            var type = conventions.GetTypeString(p.Type, p);
            if (p.IsOfKind(CodeParameterKind.RequestBody))
            {
                // Accept complex body objects as a JSON string
                if (p.Type is CodeType parameterType && parameterType.TypeDefinition is CodeClass) type = "string";

                // Use FileInfo for stream body
                if (conventions.StreamTypeName.Equals(p.Type?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    type = FileParamType;
                    p.Name = InputFileParamName;
                }
            }
            return (type, NormalizeToIdentifier(p.Name), p);
        }).DistinctBy(static p => p.name).ToList();
        var availableOptions = WriteExecutableCommandOptions(writer, parameters);

        var isHandlerVoid = conventions.VoidTypeName.Equals(originalMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
        var returnType = conventions.GetTypeString(originalMethod.ReturnType, originalMethod);

        AddCustomCommandOptions(writer, ref availableOptions, ref parameters, returnType, isHandlerVoid, originalMethod.PagingInformation != null);

        if (!isHandlerVoid && !conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
        {
            if (!conventions.IsPrimitiveType(returnType))
            {
                // Add output filter param
                parameters.Add((OutputFilterParamType, OutputFilterParamName, null));
                availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetService(typeof({OutputFilterParamType})) as {OutputFilterParamType} ?? throw new ArgumentNullException(\"{OutputFilterParamName}\")");
            }

            // Add output formatter factory param
            parameters.Add((OutputFormatterFactoryParamType, OutputFormatterFactoryParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetService(typeof({OutputFormatterFactoryParamType})) as {OutputFormatterFactoryParamType} ?? throw new ArgumentNullException(\"{OutputFormatterFactoryParamName}\")");
        }

        if (originalMethod.PagingInformation != null)
        {
            // Add paging service param
            parameters.Add((PagingServiceParamType, PagingServiceParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetService(typeof({PagingServiceParamType})) as {PagingServiceParamType} ?? throw new ArgumentNullException(\"{PagingServiceParamName}\")");
        }

        // Add CancellationToken param
        parameters.Add((CancellationTokenParamType, CancellationTokenParamName, null));
        availableOptions.Add($"{InvocationContextParamName}.GetCancellationToken()");

        // Add RequestAdapter param
        parameters.Add((RequestAdapterParamType, RequestAdapterParamName, null));
        availableOptions.Add($"{InvocationContextParamName}.GetRequestAdapter()");

        writer.WriteLine($"{CommandVariableName}.SetHandler(async ({InvocationContextParamName}) => {{");
        writer.IncreaseIndent();
        for (var i = 0; i < availableOptions.Count; i++)
        {
            var (paramType, paramName, _) = parameters[i];
            var op = availableOptions[i];
            var isRequiredService = op.Contains($"GetService(typeof({paramType})) as {paramType} ?? throw new ArgumentNullException(\"{paramName}\")", StringComparison.Ordinal);
            var typeName = isRequiredService ? paramType : "var";
            writer.WriteLine($"{typeName} {paramName.ToFirstCharacterLowerCase()} = {availableOptions[i]};");
        }

        WriteCommandHandlerBody(originalMethod, parentClass, requestParams, isHandlerVoid, returnType, writer);
        // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
        WriteCommandHandlerBodyOutput(writer, originalMethod, isHandlerVoid);
        writer.CloseBlock("});");
        writer.WriteLine($"return {CommandVariableName};");
    }

    private (IEnumerable<CodeMethod> includedSubCommands, string? builderVarName) InitializeSharedCommand(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string name)
    {
        string? builderVarName = null;
        // If there's a matching command with the same name as this
        // command, use its command builder instead of creating a new command.
        // This reduces the probability of duplicate subcommand names
        // e.g. if we have a route like GET /tests, the generated command will
        // be "mgc tests list". Say we have another route that's
        // GET /tests/{test-id}/list. In this case, we want the command that
        // reaches this endpoint to be "mgc tests list get". If we didn't use
        // this technique, the generated command would actually be "mgc tests
        // list list get".
        var commandInfo = GetCommandBuilderFromIndexer(codeElement, writer, parentClass);
        string initializer;
        if (commandInfo.HasValue)
        {
            var (builderName, method) = commandInfo.Value;
            initializer = $"{builderName}.{NormalizeToIdentifier(method.Name)}()";
            builderVarName = NormalizeToIdentifier(builderName);
        }
        else
        {
            // Try reusing nav commands too. e.g. GET /tests and GET /tests/list/
            // Should resolve to mgc tests list and mgc tests list get respectively.
            var navCmd = GetCommandBuilderFromNavProperties(codeElement, parentClass);

            if (navCmd is not null)
            {
                initializer = $"{navCmd.Name}()";
            }
            else
            {
                initializer = $"new Command(\"{name}\")";
            }
        }
        writer.WriteLine($"var {CommandVariableName} = {initializer};");
        WriteCommandDescription(codeElement, writer);

        var includedSubCommands = AddMatchingIndexerCommandsAsSubCommands(codeElement, writer, parentClass, builderVarName ?? BuilderInstanceName, commandInfo?.method).ToArray();
        return (includedSubCommands, builderVarName);
    }

    private (string builderName, CodeMethod method)? GetCommandBuilderFromIndexer(CodeMethod codeElement, LanguageWriter writer, CodeClass parent)
    {
        // We match based on SimpleName which should contain at least one valid identifier character.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName)) return null;
        // Assumption is that there can only be 1 indexer per code class. This code will throw if
        // multiple indexers exist.
        var indexer = parent.UnorderedMethods
                .SingleOrDefault(static m => m.OriginalIndexer != null)?.OriginalIndexer;

        if (indexer is null) return null;
        if (indexer.ReturnType.AllTypes.First().TypeDefinition is not CodeClass td) return null;

        // Find the first non-list indexer command that matches by the name
        // The actual command names will not be the same as the SimpleName.
        // There shouldn't be more than 1 match that is a non-list command.
        var match = td.UnorderedMethods
                .Where(static m => !string.Equals(m.ReturnType.Name, IndexerReturn, StringComparison.Ordinal) && m.HttpMethod == null) // Guard against pulling in executable commands.
                .SingleOrDefault(m => m.IsOfKind(CodeMethodKind.CommandBuilder) && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase));
        // If there are no commands in this indexer that match a command in the current class, skip the indexer.
        if (match is null) return null;

        var targetClass = conventions.GetTypeString(indexer.ReturnType, codeElement);
        var builderName = NormalizeToIdentifier(indexer.Name).ToFirstCharacterLowerCase();

        AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {builderName} = ", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));

        return (builderName, match);
    }

#pragma warning disable CA1822
    private CodeMethod? GetCommandBuilderFromNavProperties(CodeMethod codeElement, CodeClass parent)
    {
#pragma warning restore CA1822
        // We match based on SimpleName which should contain at least one valid
        // identifier character in the name.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName)) return null;
        // Assumption is that there can only be 1 nav property with a specific
        // name per code class. This code will throw if multiple nav properties
        // exist with the same name.
        return parent.UnorderedMethods
                .SingleOrDefault(m =>
                {
                    return m != codeElement
                        && m.IsOfKind(CodeMethodKind.CommandBuilder)
                        && m.AccessedProperty != null
                        && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase);
                });
    }

    private IEnumerable<CodeMethod> AddMatchingIndexerCommandsAsSubCommands(CodeMethod codeElement, LanguageWriter writer, CodeClass parent, string builderName, CodeMethod? exclude = null)
    {
        // We match based on SimpleName which should contain at least one valid identifier character.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName)) return Enumerable.Empty<CodeMethod>();

        // A code class should only have 1 indexer. If there's more than 1 indexer, this code will fail.
        var indexer = parent.UnorderedMethods
                .SingleOrDefault(m => m.OriginalIndexer != null)?.OriginalIndexer;
        if (indexer is null) return Enumerable.Empty<CodeMethod>();
        if (indexer.ReturnType.AllTypes.First().TypeDefinition is not CodeClass td) return Enumerable.Empty<CodeMethod>();

        var matches = td.Methods
            .Where(m => m != exclude && m.IsOfKind(CodeMethodKind.CommandBuilder) && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase));

        // If there are no commands in this indexer that match a command in the current class, skip the indexer.
        if (!matches.Any()) return Enumerable.Empty<CodeMethod>();

        if (exclude is null)
        {
            var targetClass = conventions.GetTypeString(indexer.ReturnType, codeElement);
            AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {builderName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        }

        return matches;
    }

    private void AddCustomCommandOptions(LanguageWriter writer, ref List<string> availableOptions, ref List<(string, string, CodeParameter?)> parameters, string returnType, bool isHandlerVoid, bool isPageable)
    {
        if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
        {
            var fileOptionName = $"{NormalizeToIdentifier(OutputFileParamName)}Option";
            writer.WriteLine($"var {fileOptionName} = new Option<{FileParamType}>(\"--{NormalizeToOption(OutputFileParamName)}\");");
            writer.WriteLine($"{CommandVariableName}.AddOption({fileOptionName});");
            parameters.Add((FileParamType, OutputFileParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({fileOptionName})");
        }
        else if (!isHandlerVoid && !conventions.IsPrimitiveType(returnType))
        {
            // Add output type param
            var outputOptionName = "outputOption";
            writer.WriteLine($"var {outputOptionName} = new Option<{OutputFormatParamType}>(\"--{OutputFormatParamName}\", () => FormatterType.JSON);");
            writer.WriteLine($"{CommandVariableName}.AddOption({outputOptionName});");
            parameters.Add((OutputFormatParamType, OutputFormatParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({outputOptionName})");

            // Add output filter query param
            var outputFilterQueryOptionName = $"{OutputFilterQueryParamName}Option";
            writer.WriteLine($"var {outputFilterQueryOptionName} = new Option<{OutputFilterQueryParamType}>(\"--{OutputFilterQueryParamName}\");");
            writer.WriteLine($"{CommandVariableName}.AddOption({outputFilterQueryOptionName});");
            parameters.Add((OutputFilterQueryParamType, OutputFilterQueryParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({outputFilterQueryOptionName})");

            // Add --all option for pageable data
            if (isPageable)
            {
                var allOptionName = $"{AllParamName}Option";
                writer.WriteLine($"var {allOptionName} = new Option<{AllParamType}>(\"--{AllParamName}\");");
                writer.WriteLine($"{CommandVariableName}.AddOption({allOptionName});");
                parameters.Add((AllParamType, AllParamName, null));
                availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({allOptionName})");
            }
        }
    }

    private void WriteCommandHandlerBodyOutput(LanguageWriter writer, in CodeMethod originalMethod, bool isHandlerVoid)
    {
        if (isHandlerVoid)
        {
            writer.WriteLine("Console.WriteLine(\"Success\");");
        }
        else
        {
            var formatterVar = "formatter";

            if (originalMethod.PagingInformation != null)
            {
                writer.WriteLine(CSharpConventionService.NullableEnableDirective, false);
                writer.WriteLine($"IOutputFormatter? {formatterVar} = null;");
                writer.WriteLine(CSharpConventionService.NullableRestoreDirective, false);
            }
            if (originalMethod.ReturnType is CodeType type &&
                conventions.GetTypeString(type, originalMethod) is { } typeString && !typeString.Equals("Stream", StringComparison.Ordinal))
            {
                var formatterTypeVal = "FormatterType.TEXT";
                if (!conventions.IsPrimitiveType(typeString))
                {
                    if (originalMethod.PagingInformation != null)
                    {
                        // Special handling for pageable requests
                        writer.WriteLine("if (pageResponse?.StatusCode >= 200 && pageResponse?.StatusCode < 300) {");
                        writer.IncreaseIndent();
                        writer.WriteLine($"{formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter({OutputFormatParamName});");
                    }
                    formatterTypeVal = OutputFormatParamName;
                    string canFilterExpr = $"(response != Stream.Null)";
                    writer.WriteLine($"response = {canFilterExpr} ? await {OutputFilterParamName}.FilterOutputAsync(response, {OutputFilterQueryParamName}, {CancellationTokenParamName}) : response;");

                    if (originalMethod.PagingInformation != null)
                    {
                        writer.CloseBlock("} else {");
                        writer.IncreaseIndent();
                        writer.WriteLine($"{formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter(FormatterType.TEXT);");
                        writer.CloseBlock();
                    }
                }

                if (originalMethod.PagingInformation == null)
                {
                    writer.WriteLine($"var {formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter({formatterTypeVal});");
                }

                writer.WriteLine($"await {formatterVar}.WriteOutputAsync(response, {CancellationTokenParamName});");
            }
            else
            {
                writer.WriteLine($"if ({OutputFileParamName} == null) {{");
                writer.IncreaseIndent();
                writer.WriteLine("using var reader = new StreamReader(response);");
                writer.WriteLine("var strContent = reader.ReadToEnd();");
                writer.WriteLine("Console.Write(strContent);");
                writer.CloseBlock();
                writer.WriteLine("else {");
                writer.IncreaseIndent();
                writer.WriteLine($"using var writeStream = {OutputFileParamName}.OpenWrite();");
                writer.WriteLine("await response.CopyToAsync(writeStream);");
                writer.WriteLine($"Console.WriteLine($\"Content written to {{{OutputFileParamName}.FullName}}.\");");
                writer.CloseBlock();
            }
        }
    }

    private static List<string> WriteExecutableCommandOptions(LanguageWriter writer, List<(string, string, CodeParameter?)> parametersList)
    {
        var availableOptions = new List<string>();
        foreach (var (optionType, name, option) in parametersList.Where(static x => x.Item3 != null))
        {
            var optionName = $"{name.ToFirstCharacterLowerCase()}Option";

            var optionBuilder = new StringBuilder("new Option");
            if (!string.IsNullOrEmpty(optionType))
            {
                optionBuilder.Append(CultureInfo.InvariantCulture, $"<{optionType}>");
            }
            optionBuilder.Append("(\"");
            if (name.Length > 1) optionBuilder.Append('-');
            optionBuilder.Append(CultureInfo.InvariantCulture, $"-{NormalizeToOption(option!.Name)}\"");
            if (option.Kind == CodeParameterKind.RequestBodyContentType)
            {
                option.DefaultValue = option.PossibleValues.Count > 0 ? option.PossibleValues[0] : string.Empty;
                option.Optional = true;
            }
            if (!string.IsNullOrEmpty(option.DefaultValue))
            {
                var defaultValue = optionType == "string" ? $"\"{option.DefaultValue}\"" : option.DefaultValue;
                optionBuilder.Append(CultureInfo.InvariantCulture, $", getDefaultValue: ()=> {defaultValue}");
            }

            var builder = BuildDescriptionForElement(option);

            if (builder?.Length > 0)
            {
                optionBuilder.Append(CultureInfo.InvariantCulture, $", description: \"{builder}\"");
            }

            optionBuilder.Append(") {");
            var strValue = $"{optionBuilder}";
            writer.WriteLine($"var {optionName} = {strValue}");
            writer.IncreaseIndent();
            var isRequired = !option.Optional || option.IsOfKind(CodeParameterKind.Path);

            if (option.Type.IsCollection)
            {
                var arity = isRequired ? "OneOrMore" : "ZeroOrMore";
                writer.WriteLine($"Arity = ArgumentArity.{arity}");
            }

            writer.DecreaseIndent();
            writer.WriteLine("};");
            writer.WriteLine($"{optionName}.IsRequired = {isRequired.ToString().ToFirstCharacterLowerCase()};");
            writer.WriteLine($"{CommandVariableName}.AddOption({optionName});");
            var suffix = string.Empty;
            if (option.IsOfKind(CodeParameterKind.RequestBody) && !FileParamType.Equals(optionType, StringComparison.OrdinalIgnoreCase))
            {
                suffix = " ?? string.Empty";
            }
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({optionName}){suffix}");
        }

        return availableOptions;
    }

    private static void WriteCommandDescription(CodeMethod codeElement, LanguageWriter writer)
    {
        var builder = BuildDescriptionForElement(codeElement);
        if (builder?.Length > 0)
            writer.WriteLine($"{CommandVariableName}.Description = \"{builder}\";");
    }

    private static StringBuilder? BuildDescriptionForElement(CodeElement element)
    {
        var documentation = element switch
        {
            CodeMethod doc => doc.Documentation,
            CodeProperty prop => prop.Documentation,
            CodeIndexer prop => prop.Documentation,
            CodeParameter prop => prop.Documentation,
            _ => null,
        };
        // Optimization, don't allocate
        if (documentation is null) return null;
        var builder = new StringBuilder();
        if (documentation.DescriptionAvailable)
        {
            builder.Append(documentation.GetDescription(static type => type.Name));
        }

        // Add content type values to description.
        if (element is CodeParameter cp && cp.Kind is CodeParameterKind.RequestBodyContentType && cp.PossibleValues.Count > 1)
        {
            if (builder.Length > 0)
            {
                builder.Append(@"\n");
            }
            builder.Append("Allowed values: ");
            foreach (var value in cp.PossibleValues)
            {
                builder.Append(@"\n  - ");
                builder.Append(value);
            }
        }

        if (documentation.DocumentationLink is not null)
        {
            string newLine = string.Empty;
            if (documentation.DescriptionAvailable)
            {
                newLine = element switch
                {
                    CodeParameter => @"\n",
                    _ => @"\n\n",
                };
            }
            string title;
            if (!string.IsNullOrWhiteSpace(documentation.DocumentationLabel))
            {
                title = documentation.DocumentationLabel;
            }
            else
            {
                title = element is CodeParameter ? "See" : "Related Links";
            }
            string titleSuffix = element switch
            {
                CodeParameter => ": ",
                _ => ":\\n  ",
            };

            builder.Append(newLine);
            builder.Append(title);
            builder.Append(titleSuffix);
            builder.Append(documentation.DocumentationLink);
        }

        return builder;
    }

    private void WriteNavCommand(CodeMethod codeElement, LanguageWriter writer, CodeClass parent, string name)
    {
        if (((codeElement.AccessedProperty?.Type ?? codeElement.OriginalMethod?.ReturnType) is CodeType codeReturnType) && (codeReturnType.TypeDefinition is CodeClass typeDef))
        {
            var targetClass = conventions.GetTypeString(codeReturnType, codeElement);

            // If the nav property has more than 1 match, then it means it
            // has been initialized by another command builder. Filter it out.
            var builderMethods = typeDef.Methods // Already ordered by name
                .Where(static m => m.IsOfKind(CodeMethodKind.CommandBuilder))
                .GroupBy(static m => m.SimpleName, StringComparer.OrdinalIgnoreCase)
                .SelectMany(static m => m.Count() > 1 ? m.Where(static m1 => m1.AccessedProperty is null) : m);
            if (!builderMethods.Any()) return;

            var (includedSubCommands, matchingIndexerIdName) = InitializeSharedCommand(codeElement, parent, writer, name);
            AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {BuilderInstanceName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

            AddCommands(writer, builderMethods, includedSubCommands, matchingIndexerIdName);
        }

        writer.WriteLine($"return {CommandVariableName};");
    }

    private static void WriteRootBuildCommand(CodeMethod codeElement, LanguageWriter writer, IEnumerable<CodeMethod> orderedClassMethods)
    {
        var commandBuilderMethods = orderedClassMethods.Where(m => m.Kind == CodeMethodKind.CommandBuilder && m != codeElement);
        writer.WriteLine($"var {CommandVariableName} = new RootCommand();");
        WriteCommandDescription(codeElement, writer);
        foreach (var method in commandBuilderMethods)
        {
            writer.WriteLine($"{CommandVariableName}.AddCommand({method.Name}());");
        }

        writer.WriteLine($"return {CommandVariableName};");
    }

    private void WriteIndexerBuildCommand(CodeIndexer indexer, CodeMethod codeElement, LanguageWriter writer, CodeClass parent)
    {
        var targetClass = conventions.GetTypeString(indexer.ReturnType, codeElement);

        if (indexer.ReturnType.AllTypes.First().TypeDefinition is not CodeClass td) return;

        var parentMethodNames = parent.UnorderedMethods
            .Where(static m => m.IsOfKind(CodeMethodKind.CommandBuilder) && !string.IsNullOrWhiteSpace(m.SimpleName))
            .Select(static m => m.SimpleName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // If a method with the same name exists in the indexer's parent class, filter it.
        var builderMethods = (td.Methods
                .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder) && !parentMethodNames.Contains(m.SimpleName)))
            .ToArray();
        if (builderMethods.Length == 0)
        {
            writer.WriteLine($"return new(new(0), new(0));");
            return;
        }
        bool hasExecutable = Array.Exists(builderMethods, static m => m.HttpMethod is not null);
        bool hasNonExecutable = Array.Exists(builderMethods, static m => m.HttpMethod is null);

        if (hasExecutable)
        {
            writer.WriteLine("var executables = new List<Command>();");
        }

        if (hasNonExecutable)
        {
            writer.WriteLine("var commands = new List<Command>();");
        }

        AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {BuilderInstanceName} = ", pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));

        foreach (var method in builderMethods)
        {
            string variableName = method.HttpMethod is not null ? "executables" : "commands";
            if (method.ReturnType.IsCollection)
            {
                writer.WriteLine($"{variableName}.AddRange({BuilderInstanceName}.{method.Name}());");
            }
            else
            {
                writer.WriteLine($"{variableName}.Add({BuilderInstanceName}.{method.Name}());");
            }
        }

        var item1 = hasExecutable ? "executables" : "new(0)";
        var item2 = hasNonExecutable ? "commands" : "new(0)";
        writer.WriteLine($"return new({item1}, {item2});");
    }

    private static void AddCommandBuilderContainerInitialization(CodeClass parentClass, string returnType, LanguageWriter writer, string? prefix = default, IEnumerable<CodeParameter>? pathParameters = default)
    {
        var pathParametersProp = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        var urlTplRef = pathParametersProp?.Name.ToFirstCharacterUpperCase();
        var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", pathParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";

        writer.WriteLine($"{prefix}new {returnType}({urlTplRef}{pathParametersSuffix});");
    }

    protected virtual void WriteCommandHandlerBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        if (parentClass
                    .UnorderedMethods
                    .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod) is not CodeMethod generatorMethod) return;
        bool isStreamReq = false;
        if (requestParams.requestBody is CodeParameter requestBodyParam)
        {
            var requestBodyParamType = requestBodyParam.Type as CodeType;
            if (requestBodyParamType?.TypeDefinition is CodeClass)
            {
                writer.WriteLine($"using var stream = new MemoryStream(Encoding.UTF8.GetBytes({requestBodyParam.Name}));");
                writer.WriteLine($"var parseNode = ParseNodeFactoryRegistry.DefaultInstance.GetRootParseNode(\"{generatorMethod.RequestBodyContentType.SanitizeDoubleQuote()}\", stream);");

                var typeString = conventions.GetTypeString(requestBodyParamType, requestBodyParam, false);

                if (requestBodyParamType.IsCollection)
                {
                    writer.WriteLine($"var model = parseNode.GetCollectionOfObjectValues<{typeString}>({typeString}.CreateFromDiscriminatorValue)?.ToList();");
                }
                else
                {
                    writer.WriteLine($"var model = parseNode.GetObjectValue<{typeString}>({typeString}.CreateFromDiscriminatorValue);");
                }

                // Check for null model
                // Add logging with reason for skipped execution here
                writer.StartBlock("if (model is null) {");
                writer.WriteLine("Console.Error.WriteLine(\"No model data to send.\");");
                writer.WriteLine("return;");
                writer.CloseBlock();

                requestBodyParam.Name = "model";
            }
            else if (conventions.StreamTypeName.Equals(requestBodyParamType?.Name, StringComparison.OrdinalIgnoreCase))
            {
                isStreamReq = true;
                var pName = requestBodyParam.Name;
                requestBodyParam.Name = "stream";
                // Check for file existence
                // Add logging with reason for skipped execution here
                writer.StartBlock($"if ({pName} is null || !{pName}.Exists) {{");
                writer.WriteLine("Console.Error.WriteLine(\"No available file to send.\");");
                writer.WriteLine("return;");
                writer.CloseBlock();
                writer.WriteLine($"using var {requestBodyParam.Name} = {pName}.OpenRead();");
            }
        }

        var parametersList = string.Join(", ", new[] { requestParams.requestBody, requestParams.requestContentType }
                            .Select(static x => x?.Name).Where(static x => x != null));
        var separator = string.IsNullOrWhiteSpace(parametersList) ? "" : ", ";

        WriteRequestInformation(writer, generatorMethod, parametersList, separator, isStreamReq);

        var errorMappingVarName = "default";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"var {errorMappingVarName} = new Dictionary<string, ParsableFactory<IParsable>> {{");
            writer.IncreaseIndent();
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"{{\"{errorMapping.Key.ToUpperInvariant()}\", {errorMapping.Value.Name.ToFirstCharacterUpperCase()}.CreateFromDiscriminatorValue}},");
            }
            writer.CloseBlock("};");
        }

        var isStreamResp = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        const string SendNoContent = "SendNoContentAsync";
        const string SendStream = "SendPrimitiveAsync<Stream>";
        if (isVoid)
        {
            writer.WriteLine($"await {RequestAdapterParamName}.{SendNoContent}(requestInfo, errorMapping: {errorMappingVarName}, cancellationToken: {CancellationTokenParamName});");
        }
        else if (!isStreamResp && !conventions.IsPrimitiveType(returnType) && codeElement.PagingInformation is { } pi)
        {
            writer.WriteLine($"var pagingData = new PageLinkData(requestInfo, null, itemName: \"{pi.ItemName}\", nextLinkName: \"{pi.NextLinkName}\");");
            writer.WriteLine($"var pageResponse = await {PagingServiceParamName}.GetPagedDataAsync((info, token) => {RequestAdapterParamName}.{SendNoContent}(info, cancellationToken: token), pagingData, {AllParamName}, {CancellationTokenParamName});");
            writer.WriteLine("var response = pageResponse?.Response;");
        }
        else
        {
            // TODO: Warn when paging information is available on a stream response
            // https://github.com/microsoft/kiota/issues/4208
            writer.WriteLine($"var response = await {RequestAdapterParamName}.{SendStream}(requestInfo, errorMapping: {errorMappingVarName}, cancellationToken: {CancellationTokenParamName}) ?? Stream.Null;");
        }
    }

    private static void WriteRequestInformation(LanguageWriter writer, CodeMethod generatorMethod, string parametersList, string separator, bool isStream)
    {
        writer.WriteLine($"var requestInfo = {generatorMethod?.Name}({parametersList}{separator}q => {{");
        if (generatorMethod?.PathQueryAndHeaderParameters != null)
        {
            writer.IncreaseIndent();
            foreach (var param in generatorMethod.PathQueryAndHeaderParameters.Where(p => p.IsOfKind(CodeParameterKind.QueryParameter)))
            {
                var paramName = NormalizeToIdentifier(param.Name);
                var isStringParam = "string".Equals(param.Type.Name, StringComparison.OrdinalIgnoreCase) && !param.Type.IsCollection;
                var indentParam = true;
                if (isStringParam)
                {
                    writer.Write($"if (!string.IsNullOrEmpty({paramName})) ");
                    indentParam = false;
                }

                var paramProperty = (param.Name.EndsWith("-query", StringComparison.Ordinal) ? param.Name.Replace("-query", "", StringComparison.Ordinal) : param.Name).ToFirstCharacterUpperCase();
                writer.Write($"q.QueryParameters.{paramProperty} = {paramName};", indentParam);

                writer.WriteLine();
            }
            writer.CloseBlock("});");

            foreach (var param in generatorMethod.PathQueryAndHeaderParameters.Where(p => p.IsOfKind(CodeParameterKind.Path)))
            {
                var paramName = (string.IsNullOrEmpty(param.SerializationName) ? param.Name : param.SerializationName).SanitizeParameterNameForUrlTemplate();
                var paramIdent = NormalizeToIdentifier(param.Name).ToFirstCharacterLowerCase();
                writer.WriteLine($"if ({paramIdent} is not null) requestInfo.PathParameters.Add(\"{paramName}\", {paramIdent});");
            }

            foreach (var param in generatorMethod.PathQueryAndHeaderParameters.Where(p => p.IsOfKind(CodeParameterKind.Headers)))
            {
                var paramName = string.IsNullOrEmpty(param.SerializationName) ? param.Name : param.SerializationName;
                var paramIdent = NormalizeToIdentifier(param.Name).ToFirstCharacterLowerCase();
                writer.WriteLine($"if ({paramIdent} is not null) requestInfo.Headers.Add(\"{paramName}\", {paramIdent});");
            }

            // Set the content type header. Will not add the code if the method is a stream, has no RequestBodyContentType or if there's no body parameter.
            if (!isStream && generatorMethod.Parameters.Any(p => p.IsOfKind(CodeParameterKind.RequestBody)))
            {
                var sanitizedRequestBodyContentType = generatorMethod.RequestBodyContentType.SanitizeDoubleQuote();
                if (!string.IsNullOrWhiteSpace(sanitizedRequestBodyContentType))
                {
                    writer.WriteLine($"requestInfo.SetContentFromParsable({RequestAdapterParamName}, \"{sanitizedRequestBodyContentType}\", model);");
                }
                else
                {
                    // Being here implies a new case to handle.
                    var url = generatorMethod.Parent is CodeClass c ? c.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate)?.Name : null;
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        url = "N/A";
                    }
                    throw new InvalidOperationException($"Content for request '{generatorMethod.HttpMethod}: {url}' was not set");
                }
            }
        }
        else
        {
            writer.WriteLine("});");
        }
    }

    private static void AddCommands(LanguageWriter writer, IEnumerable<CodeMethod> builderMethods, IEnumerable<CodeMethod> includedSubCommands, string? builderName)
    {
        bool hasExecutable = false;
        bool hasNonExecutable = false;
        var methods = new List<CodeMethod>();
        methods.AddRange(builderMethods);
        methods.AddRange(includedSubCommands);
        if (methods.Count == 0) return;
        var executablesCount = methods.Count(static m => m.OriginalMethod?.HttpMethod is not null);

        if (executablesCount > 0)
        {
            writer.WriteLine($"var {ExecCommandsVariableName} = new List<Command>();");
            hasExecutable = true;
        }

        if ((methods.Count - executablesCount) > 0)
        {
            writer.WriteLine($"var {NonExecCommandsVariableName} = new List<Command>();");
            hasNonExecutable = true;
        }

        bool sortMethods = false;

        // Start with the current class' commands then the indexer commands in the item builder.
        foreach (var method in methods.OrderBy(static m => string.Equals(m.ReturnType.Name, IndexerReturn, StringComparison.Ordinal)))
        {
            if (string.Equals(method.ReturnType.Name, IndexerReturn, StringComparison.Ordinal))
            {
                // If we have any indexer sub-command, we must provide the name of the indexer
                writer.WriteLine($"var cmds = {builderName ?? BuilderInstanceName}.{method.Name}();");
                if (hasExecutable)
                {
                    writer.WriteLine($"{ExecCommandsVariableName}.AddRange(cmds.Item1);");
                }

                if (hasNonExecutable)
                {
                    sortMethods = true;
                    writer.WriteLine($"{NonExecCommandsVariableName}.AddRange(cmds.Item2);");
                }
            }
            else
            {
                string variableName;
                if (method.OriginalMethod?.HttpMethod is not null)
                {
                    variableName = ExecCommandsVariableName;
                }
                else
                {
                    variableName = NonExecCommandsVariableName;
                }
                writer.WriteLine($"{variableName}.Add({BuilderInstanceName}.{method.Name}());");
            }
        }
        if (hasExecutable)
        {
            writer.WriteLine($"foreach (var cmd in {ExecCommandsVariableName})");
            writer.StartBlock();
            writer.WriteLine($"{CommandVariableName}.AddCommand(cmd);");
            writer.CloseBlock();
        }

        if (hasNonExecutable)
        {
            var suffix = string.Empty;
            if (sortMethods)
            {
                suffix = ".OrderBy(static c => c.Name, StringComparer.Ordinal)";
            }
            writer.WriteLine($"foreach (var cmd in {NonExecCommandsVariableName}{suffix})");
            writer.StartBlock();
            writer.WriteLine($"{CommandVariableName}.AddCommand(cmd);");
            writer.CloseBlock();
        }
    }

    /// <summary>
    /// Converts delimited string into camel case for use as identifiers
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToIdentifier(string input)
    {
        return input.ToOriginalCamelCase('-', '_', '.');
    }

    /// <summary>
    /// Converts camel-case or delimited string to '-' delimited string for use as a command option
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToOption(string input)
    {
        var result = CamelCaseRegex().Replace(input, "-$1");
        // 2 passes for cases like "singleValueLegacyExtendedProperty_id"
        result = DelimitedRegex().Replace(result, "-$1");

        return result.ToLowerInvariant();
    }

    [GeneratedRegex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Singleline, 500)]
    private static partial Regex DelimitedRegex();
    [GeneratedRegex("(?<=[a-z])([A-Z])", RegexOptions.Singleline, 500)]
    private static partial Regex CamelCaseRegex();
    [GeneratedRegex("([A-Z])", RegexOptions.Singleline, 500)]
    private static partial Regex UppercaseRegex();
}
