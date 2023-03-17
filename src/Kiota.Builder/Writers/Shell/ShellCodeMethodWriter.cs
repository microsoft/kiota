﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Shell;
partial class ShellCodeMethodWriter : CodeMethodWriter
{
    private static readonly Regex delimitedRegex = ShellDelimitedRegex();
    private static readonly Regex camelCaseRegex = ShellCamelCaseRegex();
    private static readonly Regex uppercaseRegex = ShellUppercaseRegex();
    private const string AllParamType = "bool";
    private const string AllParamName = "all";
    private const string CancellationTokenParamType = "CancellationToken";
    private const string CancellationTokenParamName = "cancellationToken";
    private const string FileParamType = "FileInfo";
    private const string FileParamName = "file";
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
    private const string JsonNoIndentParamType = "bool";
    private const string JsonNoIndentParamName = "jsonNoIndent";
    private const string InvocationContextParamName = "invocationContext";
    private const string CommandVariableName = "command";

    public ShellCodeMethodWriter(CSharpConventionService conventionService) : base(conventionService)
    {
    }

    protected override void WriteCommandBuilderBody(CodeMethod codeElement, CodeClass parentClass, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
    {
        var classMethods = parentClass.Methods;
        var name = codeElement.SimpleName;
        name = uppercaseRegex.Replace(name, "-$1").TrimStart('-').ToLower();

        if (codeElement.HttpMethod == null)
        {
            if (codeElement.OriginalMethod is not null && codeElement.OriginalMethod.Kind == CodeMethodKind.ClientConstructor)
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
            .Methods
            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod) is not CodeMethod generatorMethod ||
            codeElement.OriginalMethod is not CodeMethod originalMethod) return;
        var parametersList = generatorMethod.PathQueryAndHeaderParameters.Where(static p => !string.IsNullOrWhiteSpace(p.Name)).ToList() ?? new List<CodeParameter>();
        if (originalMethod.Parameters.OfKind(CodeParameterKind.RequestBody) is CodeParameter bodyParam)
        {
            parametersList.Add(bodyParam);
        }

        InitializeSharedCommand(codeElement, parentClass, writer, name);
        writer.WriteLine("// Create options for all the parameters");
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
                    p.Name = FileParamName;
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
                availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetRequiredService<{OutputFilterParamType}>()");
            }

            // Add output formatter factory param
            parameters.Add((OutputFormatterFactoryParamType, OutputFormatterFactoryParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetRequiredService<{OutputFormatterFactoryParamType}>()");
        }

        if (originalMethod.PagingInformation != null)
        {
            // Add paging service param
            parameters.Add((PagingServiceParamType, PagingServiceParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.BindingContext.GetRequiredService<{PagingServiceParamType}>()");
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
            var isRequiredService = op.Contains($"GetRequiredService<{paramType}>");
            var typeName = isRequiredService ? paramType : "var";
            writer.WriteLine($"{typeName} {paramName.ToFirstCharacterLowerCase()} = {availableOptions[i]};");
        }

        WriteCommandHandlerBody(originalMethod, parentClass, requestParams, isHandlerVoid, returnType, writer);
        // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
        WriteCommandHandlerBodyOutput(writer, originalMethod, isHandlerVoid);
        writer.DecreaseIndent();
        writer.WriteLine("});");
        writer.WriteLine($"return {CommandVariableName};");
    }

    private void InitializeSharedCommand(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string name)
    {
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
        }
        else
        {
            // Try reusing nav commands too. e.g. GET /tests and GET /tests/list/
            // Should resolve to mgc tests lists and mgc tests list get respectively.
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
        AddMatchingIndexerCommandsAsSubCommands(codeElement, writer, parentClass, commandInfo?.method);
    }

    private (string builderName, CodeMethod method)? GetCommandBuilderFromIndexer(CodeMethod codeElement, LanguageWriter writer, CodeClass parent)
    {
        // We match based on SimpleName which should contain at least one valid identifier character.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName.CleanupSymbolName())) return null;
        // Assumption is that there can only be 1 indexer per code class. This code will throw if
        // multiple indexers exist.
        var indexer = parent.GetChildElements().OfType<CodeMethod>()
                .SingleOrDefault(m => m.OriginalIndexer != null)?.OriginalIndexer;

        if (indexer is null) return null;

        // Find the first non-list indexer command that matches by the name
        // We compare the SimpleName as is to allow tweaking names in the refiner
        // to control the outcome. The actual commands will not be the same as the
        // SimpleName.
        var match = indexer.ReturnType.AllTypes.First().TypeDefinition?.GetChildElements(true).OfType<CodeMethod>()
                .Where(m => !m.ReturnType.IsCollection && m.HttpMethod == null) // Guard against pulling in executable commands.
                .FirstOrDefault(m => m.IsOfKind(CodeMethodKind.CommandBuilder) && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase));
        // If there are no commands in this indexer that match a command in the current class, skip the indexer.
        if (match is null) return null;

        var targetClass = conventions.GetTypeString(indexer.ReturnType, codeElement);
        var builderName = NormalizeToIdentifier(indexer.Name).ToFirstCharacterLowerCase();
        AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {builderName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

        return (builderName, match);
    }

#pragma warning disable CA1822
    private CodeMethod? GetCommandBuilderFromNavProperties(CodeMethod codeElement, CodeClass parent)
    {
#pragma warning restore CA1822
        // We match based on SimpleName which should contain at least one valid
        // identifier character in the name.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName.CleanupSymbolName())) return null;
        // Assumption is that there can only be 1 nav property with a specific
        // name per code class. This code will throw if multiple nav properties
        // exist with the same name.
        var property = parent.GetChildElements().OfType<CodeMethod>()
                .SingleOrDefault(m => m != codeElement && m.IsOfKind(CodeMethodKind.CommandBuilder) && m.AccessedProperty != null && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase));

        if (property is null) return null;

        return property;
    }

    private void AddMatchingIndexerCommandsAsSubCommands(CodeMethod codeElement, LanguageWriter writer, CodeClass parent, CodeMethod? exclude = null)
    {
        // We match based on SimpleName which should contain at least one valid identifier character.
        if (string.IsNullOrWhiteSpace(codeElement.SimpleName.CleanupSymbolName())) return;
        var indexers = parent.GetChildElements().OfType<CodeMethod>()
                .Where(m => m.OriginalIndexer != null)
                .Select(m => m.OriginalIndexer!)
            ?? Enumerable.Empty<CodeIndexer>();

        foreach (var indexer in indexers)
        {
            var matches = indexer.ReturnType.AllTypes.First().TypeDefinition?.GetChildElements(true).OfType<CodeMethod>()
                    .Where(m => m != exclude && m.IsOfKind(CodeMethodKind.CommandBuilder) && string.Equals(m.SimpleName, codeElement.SimpleName, StringComparison.OrdinalIgnoreCase))
                        ?? Enumerable.Empty<CodeMethod>();
            // If there are no commands in this indexer that match a command in the current class, skip the indexer.
            if (!matches.Any()) continue;

            var targetClass = conventions.GetTypeString(indexer.ReturnType, codeElement);
            var builderName = NormalizeToIdentifier(indexer.Name);
            if (exclude is null)
            {
                AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {builderName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
            }
            foreach (var method in matches)
            {
                if (method.ReturnType.IsCollection)
                {
                    writer.WriteLine($"foreach (var {builderName}Cmd in {builderName}.{method.Name}())");
                    writer.StartBlock();
                    writer.WriteLine($"{CommandVariableName}.AddCommand({builderName}Cmd);");
                    writer.CloseBlock();
                }
                else
                {
                    writer.WriteLine($"{CommandVariableName}.AddCommand({builderName}.{method.Name}());");
                }
            }
        }
    }

    private void AddCustomCommandOptions(LanguageWriter writer, ref List<string> availableOptions, ref List<(string, string, CodeParameter?)> parameters, string returnType, bool isHandlerVoid, bool isPageable)
    {
        if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
        {
            var fileOptionName = "fileOption";
            writer.WriteLine($"var {fileOptionName} = new Option<{FileParamType}>(\"--{FileParamName}\");");
            writer.WriteLine($"{CommandVariableName}.AddOption({fileOptionName});");
            parameters.Add((FileParamType, FileParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({fileOptionName})");
        }
        else if (!isHandlerVoid && !conventions.IsPrimitiveType(returnType))
        {
            // Add output type param
            var outputOptionName = "outputOption";
            writer.WriteLine($"var {outputOptionName} = new Option<{OutputFormatParamType}>(\"--{OutputFormatParamName}\", () => FormatterType.JSON){{");
            writer.IncreaseIndent();
            writer.WriteLine("IsRequired = true");
            writer.CloseBlock("};");
            writer.WriteLine($"{CommandVariableName}.AddOption({outputOptionName});");
            parameters.Add((OutputFormatParamType, OutputFormatParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({outputOptionName})");

            // Add output filter query param
            var outputFilterQueryOptionName = $"{OutputFilterQueryParamName}Option";
            writer.WriteLine($"var {outputFilterQueryOptionName} = new Option<{OutputFilterQueryParamType}>(\"--{OutputFilterQueryParamName}\");");
            writer.WriteLine($"{CommandVariableName}.AddOption({outputFilterQueryOptionName});");
            parameters.Add((OutputFilterQueryParamType, OutputFilterQueryParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({outputFilterQueryOptionName})");

            // Add JSON no-indent option
            var jsonNoIndentOptionName = $"{JsonNoIndentParamName}Option";
            writer.WriteLine($"var {jsonNoIndentOptionName} = new Option<bool>(\"--{NormalizeToOption(JsonNoIndentParamName)}\", r => {{");
            writer.IncreaseIndent();
            writer.WriteLine("if (bool.TryParse(r.Tokens.Select(t => t.Value).LastOrDefault(), out var value)) {");
            writer.IncreaseIndent();
            writer.WriteLine("return value;");
            writer.CloseBlock();
            writer.WriteLine("return true;");
            writer.DecreaseIndent();
            writer.WriteLine("}, description: \"Disable indentation for the JSON output formatter.\");");
            writer.WriteLine($"{CommandVariableName}.AddOption({jsonNoIndentOptionName});");
            parameters.Add((JsonNoIndentParamType, JsonNoIndentParamName, null));
            availableOptions.Add($"{InvocationContextParamName}.ParseResult.GetValueForOption({jsonNoIndentOptionName})");

            // Add --all option
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

    private void WriteCommandHandlerBodyOutput(LanguageWriter writer, CodeMethod originalMethod, bool isHandlerVoid)
    {
        if (isHandlerVoid)
        {
            writer.WriteLine("Console.WriteLine(\"Success\");");
        }
        else
        {
            var formatterVar = "formatter";

            var formatterOptionsVar = "formatterOptions";
            if (originalMethod.PagingInformation != null)
            {
                writer.WriteLine($"IOutputFormatterOptions? {formatterOptionsVar} = null;");
                writer.WriteLine($"IOutputFormatter? {formatterVar} = null;");
            }
            if (originalMethod.ReturnType is CodeType type &&
                conventions.GetTypeString(type, originalMethod) is string typeString && !typeString.Equals("Stream", StringComparison.Ordinal))
            {
                var formatterTypeVal = "FormatterType.TEXT";
                if (conventions.IsPrimitiveType(typeString))
                {
                    formatterOptionsVar = "null";
                }
                else
                {
                    if (originalMethod?.PagingInformation != null)
                    {
                        // Special handling for pageable requests
                        writer.WriteLine("if (pageResponse?.StatusCode >= 200 && pageResponse?.StatusCode < 300) {");
                        writer.IncreaseIndent();
                        writer.WriteLine($"{formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter({OutputFormatParamName});");
                    }
                    formatterTypeVal = OutputFormatParamName;
                    string canFilterExpr = $"(response != Stream.Null)";
                    writer.WriteLine($"response = {canFilterExpr} ? await {OutputFilterParamName}.FilterOutputAsync(response, {OutputFilterQueryParamName}, {CancellationTokenParamName}) : response;");
                    if (originalMethod?.PagingInformation == null)
                    {
                        writer.Write("var ");
                    }
                    writer.Write($"{formatterOptionsVar} = {OutputFormatParamName}.GetOutputFormatterOptions(new FormatterOptionsModel(!{JsonNoIndentParamName}));", originalMethod?.PagingInformation != null);
                    writer.WriteLine();

                    if (originalMethod?.PagingInformation != null)
                    {
                        writer.CloseBlock("} else {");
                        writer.IncreaseIndent();
                        writer.WriteLine($"{formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter(FormatterType.TEXT);");
                        writer.CloseBlock();
                    }
                }

                if (originalMethod?.PagingInformation == null)
                {
                    writer.WriteLine($"var {formatterVar} = {OutputFormatterFactoryParamName}.GetFormatter({formatterTypeVal});");
                }

                writer.WriteLine($"await {formatterVar}.WriteOutputAsync(response, {formatterOptionsVar}, {CancellationTokenParamName});");
            }
            else
            {
                writer.WriteLine($"if ({FileParamName} == null) {{");
                writer.IncreaseIndent();
                writer.WriteLine("using var reader = new StreamReader(response);");
                writer.WriteLine("var strContent = reader.ReadToEnd();");
                writer.WriteLine("Console.Write(strContent);");
                writer.CloseBlock();
                writer.WriteLine("else {");
                writer.IncreaseIndent();
                writer.WriteLine($"using var writeStream = {FileParamName}.OpenWrite();");
                writer.WriteLine("await response.CopyToAsync(writeStream);");
                writer.WriteLine($"Console.WriteLine($\"Content written to {{{FileParamName}.FullName}}.\");");
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
                optionBuilder.Append($"<{optionType}>");
            }
            optionBuilder.Append("(\"");
            if (name.Length > 1) optionBuilder.Append('-');
            optionBuilder.Append($"-{NormalizeToOption(option!.Name)}\"");
            if (!string.IsNullOrEmpty(option.DefaultValue))
            {
                var defaultValue = optionType == "string" ? $"\"{option.DefaultValue}\"" : option.DefaultValue;
                optionBuilder.Append($", getDefaultValue: ()=> {defaultValue}");
            }

            var builder = BuildDescriptionForElement(option);

            if (builder?.Length > 0)
            {
                optionBuilder.Append($", description: \"{builder}\"");
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
            CodeMethod doc when element is CodeMethod => doc.Documentation,
            CodeProperty prop when element is CodeProperty => prop.Documentation,
            CodeIndexer prop when element is CodeIndexer => prop.Documentation,
            CodeParameter prop when element is CodeParameter => prop.Documentation,
            _ => null,
        };
        // Optimization, don't allocate
        if (documentation is null) return null;
        var builder = new StringBuilder();
        if (documentation.DescriptionAvailable)
        {
            builder.Append(documentation.Description);
        }

        if (documentation.DocumentationLink is not null)
        {
            string newLine = string.Empty;
            if (documentation.DescriptionAvailable)
            {
                newLine = element switch
                {
                    _ when element is CodeParameter => "\\n",
                    _ => "\\n\\n",
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
                _ when element is CodeParameter => ": ",
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
        InitializeSharedCommand(codeElement, parent, writer, name);

        if ((codeElement.AccessedProperty?.Type) is CodeType codeReturnType)
        {
            var targetClass = conventions.GetTypeString(codeReturnType, codeElement);

            var builderMethods = codeReturnType.TypeDefinition?.GetChildElements(true).OfType<CodeMethod>()
                .Where(static m => m.IsOfKind(CodeMethodKind.CommandBuilder))
                .OrderBy(static m => m.Name)
                .ThenBy(static m => m.ReturnType.IsCollection) ??
                Enumerable.Empty<CodeMethod>();
            AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {BuilderInstanceName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

            var duplicates = builderMethods
                .Where(m => !string.IsNullOrWhiteSpace(m.SimpleName.CleanupSymbolName()))
                .Select(m => m.SimpleName)
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var method in builderMethods)
            {
                // If the nav property has more than 1 match, then it means it
                // has been initialized by another command builder. Skip it.
                if (method.AccessedProperty is not null && duplicates.Contains(method.SimpleName)) continue;
                if (method.ReturnType.IsCollection)
                {
                    writer.WriteLine($"foreach (var cmd in {BuilderInstanceName}.{method.Name}())");
                    writer.StartBlock();
                    writer.WriteLine($"{CommandVariableName}.AddCommand(cmd);");
                    writer.CloseBlock();
                }
                else
                {
                    writer.WriteLine($"{CommandVariableName}.AddCommand({BuilderInstanceName}.{method.Name}());");
                }
            }
            // SubCommands
        }

        writer.WriteLine($"return {CommandVariableName};");
    }

    private static void WriteRootBuildCommand(CodeMethod codeElement, LanguageWriter writer, IEnumerable<CodeMethod> classMethods)
    {
        var commandBuilderMethods = classMethods.Where(m => m.Kind == CodeMethodKind.CommandBuilder && m != codeElement).OrderBy(m => m.Name);
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

        AddCommandBuilderContainerInitialization(parent, targetClass, writer, prefix: $"var {BuilderInstanceName} = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
        writer.WriteLine("var commands = new List<Command>();");

        var builderMethods = indexer.ReturnType.AllTypes.First().TypeDefinition?.GetChildElements(true).OfType<CodeMethod>()
            .Where(static m => m.IsOfKind(CodeMethodKind.CommandBuilder))
            .OrderBy(static m => m.Name) ??
            Enumerable.Empty<CodeMethod>();
        var parentMethodNames = parent.Methods
            .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder) && !string.IsNullOrWhiteSpace(m.SimpleName.CleanupSymbolName()))
            .Select(m => m.SimpleName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var method in builderMethods)
        {
            // If a method with the same name exists in the indexer's parent class, skip it.
            if (parentMethodNames.Contains(method.SimpleName)) continue;
            if (method.ReturnType.IsCollection)
            {
                writer.WriteLine($"commands.AddRange({BuilderInstanceName}.{method.Name}());");
            }
            else
            {
                writer.WriteLine($"commands.Add({BuilderInstanceName}.{method.Name}());");
            }
        }

        writer.WriteLine("return commands;");
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
                    .Methods
                    .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod) is not CodeMethod generatorMethod) return;
        if (requestParams.requestBody is CodeParameter requestBodyParam)
        {
            var requestBodyParamType = requestBodyParam.Type as CodeType;
            if (requestBodyParamType?.TypeDefinition is CodeClass)
            {
                writer.WriteLine($"using var stream = new MemoryStream(Encoding.UTF8.GetBytes({requestBodyParam.Name}));");
                writer.WriteLine($"var parseNode = ParseNodeFactoryRegistry.DefaultInstance.GetRootParseNode(\"{generatorMethod.RequestBodyContentType}\", stream);");

                var typeString = conventions.GetTypeString(requestBodyParamType, requestBodyParam, false);

                if (requestBodyParamType.IsCollection)
                {
                    writer.WriteLine($"var model = parseNode.GetCollectionOfObjectValues<{typeString}>({typeString}.CreateFromDiscriminatorValue);");
                }
                else
                {
                    writer.WriteLine($"var model = parseNode.GetObjectValue<{typeString}>({typeString}.CreateFromDiscriminatorValue);");
                }

                // Check for null model
                // Add logging with reason for skipped execution here
                writer.WriteLine($"if (model is null) return; // Cannot create a POST request from a null model.");

                requestBodyParam.Name = "model";
            }
            else if (conventions.StreamTypeName.Equals(requestBodyParamType?.Name, StringComparison.OrdinalIgnoreCase))
            {
                var pName = requestBodyParam.Name;
                requestBodyParam.Name = "stream";
                // Check for file existence
                // Add logging with reason for skipped execution here
                writer.WriteLine($"if ({pName} is null || !{pName}.Exists) return;");
                writer.WriteLine($"using var {requestBodyParam.Name} = {pName}.OpenRead();");
            }
        }

        var parametersList = string.Join(", ", new[] { requestParams.requestBody, requestParams.requestConfiguration }
                            .Select(static x => x?.Name).Where(static x => x != null));
        var separator = string.IsNullOrWhiteSpace(parametersList) ? "" : ", ";
        WriteRequestInformation(writer, generatorMethod, parametersList, separator);

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

        var requestMethod = "SendPrimitiveAsync<Stream>";
        var pageInfo = codeElement?.PagingInformation;
        if (isVoid || pageInfo != null)
        {
            requestMethod = "SendNoContentAsync";
        }

        if (pageInfo != null)
        {
            writer.WriteLine($"var pagingData = new PageLinkData(requestInfo, null, itemName: \"{pageInfo.ItemName}\", nextLinkName: \"{pageInfo.NextLinkName}\");");
            writer.WriteLine($"{(isVoid ? string.Empty : "var pageResponse = ")}await {PagingServiceParamName}.GetPagedDataAsync((info, token) => {RequestAdapterParamName}.{requestMethod}(info, cancellationToken: token), pagingData, {AllParamName}, {CancellationTokenParamName});");
            writer.WriteLine("var response = pageResponse?.Response;");
        }
        else
        {
            string suffix = string.Empty;
            if (!isVoid) suffix = " ?? Stream.Null";
            writer.WriteLine($"{(isVoid ? string.Empty : "var response = ")}await {RequestAdapterParamName}.{requestMethod}(requestInfo, errorMapping: {errorMappingVarName}, cancellationToken: {CancellationTokenParamName}){suffix};");
        }
    }

    private static void WriteRequestInformation(LanguageWriter writer, CodeMethod generatorMethod, string parametersList, string separator)
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

                var paramProperty = (param.Name.EndsWith("-query") ? param.Name.Replace("-query", "") : param.Name).ToFirstCharacterUpperCase();
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

            // Set the content type header. Will not add the code if the method has no RequestBodyContentType or if there's no body parameter.
            if (generatorMethod.Parameters.Any(p => p.IsOfKind(CodeParameterKind.RequestBody)) && !string.IsNullOrWhiteSpace(generatorMethod.RequestBodyContentType))
            {
                writer.WriteLine($"requestInfo.SetContentFromParsable({RequestAdapterParamName}, \"{generatorMethod.RequestBodyContentType}\", model);");
            }
        }
        else
        {
            writer.WriteLine("});");
        }
    }

    /// <summary>
    /// Converts delimited string into camel case for use as identifiers
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToIdentifier(string input)
    {
        return input.ToCamelCase('-', '_', '.');
    }

    /// <summary>
    /// Converts camel-case or delimited string to '-' delimited string for use as a command option
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormalizeToOption(string input)
    {
        var result = camelCaseRegex.Replace(input, "-$1");
        // 2 passes for cases like "singleValueLegacyExtendedProperty_id"
        result = delimitedRegex.Replace(result, "-$1");

        return result.ToLower();
    }

    [GeneratedRegex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex ShellDelimitedRegex();
    [GeneratedRegex("(?<=[a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex ShellCamelCaseRegex();
    [GeneratedRegex("([A-Z])", RegexOptions.Compiled)]
    private static partial Regex ShellUppercaseRegex();
}
