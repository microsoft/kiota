﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Shell
{
    class ShellCodeMethodWriter : CodeMethodWriter
    {
        private static Regex delimitedRegex = new Regex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Compiled);
        private static Regex camelCaseRegex = new Regex("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
        private static Regex uppercaseRegex = new Regex("([A-Z])", RegexOptions.Compiled);
        private const string allParamType = "bool";
        private const string allParamName = "all";
        private const string cancellationTokenParamType = "CancellationToken";
        private const string cancellationTokenParamName = "cancellationToken";
        private const string fileParamType = "FileInfo";
        private const string fileParamName = "file";
        private const string outputFilterParamType = "IOutputFilter";
        private const string outputFilterParamName = "outputFilter";
        private const string outputFilterQueryParamType = "string";
        private const string outputFilterQueryParamName = "query";
        private const string outputFormatParamType = "FormatterType";
        private const string outputFormatParamName = "output";
        private const string outputFormatterFactoryParamType = "IOutputFormatterFactory";
        private const string outputFormatterFactoryParamName = "outputFormatterFactory";
        private const string pagingServiceParamType = "IPagingService";
        private const string pagingServiceParamName = "pagingService";
        private const string jsonNoIndentParamType = "bool";
        private const string jsonNoIndentParamName = "jsonNoIndent";
        private const string invocationContextParamName = "invocationContext";

        public ShellCodeMethodWriter(CSharpConventionService conventionService) : base(conventionService)
        {
        }

        protected override void WriteCommandBuilderBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            var parent = codeElement.Parent as CodeClass;
            var classMethods = parent.Methods;
            var name = codeElement.SimpleName;
            name = uppercaseRegex.Replace(name, "-$1").TrimStart('-').ToLower();

            if (codeElement.HttpMethod == null)
            {
                // Build method
                // Puts together the BuildXXCommand objects. Needs a nav property name e.g. users
                // Command("users") -> Command("get")
                if (string.IsNullOrWhiteSpace(name))
                {
                    // BuildCommand function
                    WriteUnnamedBuildCommand(codeElement, writer, parent, classMethods);
                }
                else
                {
                    WriteContainerCommand(codeElement, writer, parent, name);
                }
            }
            else
            {
                WriteExecutableCommand(codeElement, requestParams, writer, name);
            }
        }

        private void WriteExecutableCommand(CodeMethod codeElement, RequestParams requestParams, LanguageWriter writer, string name)
        {
            var generatorMethod = (codeElement.Parent as CodeClass)
                                           .Methods
                                           .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var pathAndQueryParams = generatorMethod.PathQueryAndHeaderParameters;
            var originalMethod = codeElement.OriginalMethod;
            var origParams = originalMethod.Parameters;
            var parametersList = pathAndQueryParams?.Where(p => !string.IsNullOrWhiteSpace(p.Name))?.ToList() ?? new List<CodeParameter>();
            if (origParams.Any(p => p.IsOfKind(CodeParameterKind.RequestBody)))
            {
                var bodyParam = origParams.OfKind(CodeParameterKind.RequestBody);
                parametersList.Add(bodyParam);
            }
            writer.WriteLine($"var command = new Command(\"{name}\");");
            WriteCommandDescription(codeElement, writer);
            writer.WriteLine("// Create options for all the parameters");
            // investigate exploding query params
            // Check the possible formatting options for headers in a cli.
            // -h A=b -h
            // -h A:B,B:C
            // -h {"A": "B"}
            // parameters: (type, name, CodeParameter)
            var parameters = parametersList.Select(p =>
            {
                var type = conventions.GetTypeString(p.Type, p);
                if (p.IsOfKind(CodeParameterKind.RequestBody))
                {
                    // Accept complex body objects as a JSON string
                    if (p.Type is CodeType parameterType && parameterType.TypeDefinition is CodeClass) type = "string";

                    // Use FileInfo for stream body
                    if (conventions.StreamTypeName.Equals(p.Type?.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        type = fileParamType;
                        p.Name = fileParamName;
                    }
                }
                return (type, NormalizeToIdentifier(p.Name), p);
            }).ToList();
            var availableOptions = WriteExecutableCommandOptions(writer, parameters);

            var isHandlerVoid = conventions.VoidTypeName.Equals(originalMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
            var returnType = conventions.GetTypeString(originalMethod.ReturnType, originalMethod);

            AddCustomCommandOptions(writer, ref availableOptions, ref parameters, returnType, isHandlerVoid, originalMethod.PagingInformation != null);

            if (!isHandlerVoid && !conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
            {
                if (!conventions.IsPrimitiveType(returnType))
                {
                    // Add output filter param
                    parameters.Add((outputFilterParamType, outputFilterParamName, null));
                    availableOptions.Add($"{invocationContextParamName}.BindingContext.GetRequiredService<{outputFilterParamType}>()");
                }

                // Add output formatter factory param
                parameters.Add((outputFormatterFactoryParamType, outputFormatterFactoryParamName, null));
                availableOptions.Add($"{invocationContextParamName}.BindingContext.GetRequiredService<{outputFormatterFactoryParamType}>()");
            }

            if (originalMethod.PagingInformation != null)
            {
                // Add paging service param
                parameters.Add((pagingServiceParamType, pagingServiceParamName, null));
                availableOptions.Add($"{invocationContextParamName}.BindingContext.GetRequiredService<{pagingServiceParamType}>()");
            }

            // Add CancellationToken param
            parameters.Add((cancellationTokenParamType, cancellationTokenParamName, null));
            availableOptions.Add($"{invocationContextParamName}.GetCancellationToken()");

            writer.WriteLine($"command.SetHandler(async ({invocationContextParamName}) => {{");
            writer.IncreaseIndent();
            for (int i = 0; i < availableOptions.Count; i++)
            {
                var (paramType, paramName, parameter) = parameters[i];
                writer.WriteLine($"var {paramName.ToFirstCharacterLowerCase()} = {availableOptions[i]};");
            }

            WriteCommandHandlerBody(originalMethod, requestParams, isHandlerVoid, returnType, writer);
            // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
            WriteCommandHandlerBodyOutput(writer, originalMethod, isHandlerVoid);
            writer.DecreaseIndent();
            writer.WriteLine($"}});");
            writer.WriteLine("return command;");
        }

        private void AddCustomCommandOptions(LanguageWriter writer, ref List<string> availableOptions, ref List<(string, string, CodeParameter)> parameters, string returnType, bool isHandlerVoid, bool isPageable)
        {
            if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
            {
                var fileOptionName = "fileOption";
                writer.WriteLine($"var {fileOptionName} = new Option<{fileParamType}>(\"--{fileParamName}\");");
                writer.WriteLine($"command.AddOption({fileOptionName});");
                parameters.Add((fileParamType, fileParamName, null));
                availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({fileOptionName})");
            }
            else if (!isHandlerVoid && !conventions.IsPrimitiveType(returnType))
            {
                // Add output type param
                var outputOptionName = "outputOption";
                writer.WriteLine($"var {outputOptionName} = new Option<{outputFormatParamType}>(\"--{outputFormatParamName}\", () => FormatterType.JSON){{");
                writer.IncreaseIndent();
                writer.WriteLine("IsRequired = true");
                writer.CloseBlock("};");
                writer.WriteLine($"command.AddOption({outputOptionName});");
                parameters.Add((outputFormatParamType, outputFormatParamName, null));
                availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({outputOptionName})");

                // Add output filter query param
                var outputFilterQueryOptionName = $"{outputFilterQueryParamName}Option";
                writer.WriteLine($"var {outputFilterQueryOptionName} = new Option<{outputFilterQueryParamType}>(\"--{outputFilterQueryParamName}\");");
                writer.WriteLine($"command.AddOption({outputFilterQueryOptionName});");
                parameters.Add((outputFilterQueryParamType, outputFilterQueryParamName, null));
                availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({outputFilterQueryOptionName})");

                // Add JSON no-indent option
                var jsonNoIndentOptionName = $"{jsonNoIndentParamName}Option";
                writer.WriteLine($"var {jsonNoIndentOptionName} = new Option<bool>(\"--{NormalizeToOption(jsonNoIndentParamName)}\", r => {{");
                writer.IncreaseIndent();
                writer.WriteLine("if (bool.TryParse(r.Tokens.Select(t => t.Value).LastOrDefault(), out var value)) {");
                writer.IncreaseIndent();
                writer.WriteLine("return value;");
                writer.CloseBlock();
                writer.WriteLine("return true;");
                writer.DecreaseIndent();
                writer.WriteLine("}, description: \"Disable indentation for the JSON output formatter.\");");
                writer.WriteLine($"command.AddOption({jsonNoIndentOptionName});");
                parameters.Add((jsonNoIndentParamType, jsonNoIndentParamName, null));
                availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({jsonNoIndentOptionName})");

                // Add --all option
                if (isPageable)
                {
                    var allOptionName = $"{allParamName}Option";
                    writer.WriteLine($"var {allOptionName} = new Option<{allParamType}>(\"--{allParamName}\");");
                    writer.WriteLine($"command.AddOption({allOptionName});");
                    parameters.Add((allParamType, allParamName, null));
                    availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({allOptionName})");
                }
            }
        }

        private void WriteCommandHandlerBodyOutput(LanguageWriter writer, CodeMethod originalMethod, bool isHandlerVoid)
        {
            if (isHandlerVoid)
            {
                writer.WriteLine($"Console.WriteLine(\"Success\");");
            }
            else
            {
                var type = originalMethod.ReturnType as CodeType;
                var typeString = conventions.GetTypeString(type, originalMethod);
                var formatterVar = "formatter";

                var formatterOptionsVar = "formatterOptions";
                if (originalMethod?.PagingInformation != null)
                {
                    writer.WriteLine($"IOutputFormatterOptions? {formatterOptionsVar} = null;");
                    writer.WriteLine($"IOutputFormatter? {formatterVar} = null;");
                }
                if (typeString != "Stream")
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
                            writer.WriteLine($"{formatterVar} = {outputFormatterFactoryParamName}.GetFormatter({outputFormatParamName});");
                        }
                        formatterTypeVal = outputFormatParamName;
                        writer.WriteLine($"response = await {outputFilterParamName}?.FilterOutputAsync(response, {outputFilterQueryParamName}, {cancellationTokenParamName}) ?? response;");
                        if (originalMethod?.PagingInformation == null)
                        {
                            writer.Write("var ");
                        }
                        writer.Write($"{formatterOptionsVar} = {outputFormatParamName}.GetOutputFormatterOptions(new FormatterOptionsModel(!{jsonNoIndentParamName}));", originalMethod?.PagingInformation != null);
                        writer.WriteLine();

                        if (originalMethod?.PagingInformation != null)
                        {
                            writer.CloseBlock("} else {");
                            writer.IncreaseIndent();
                            writer.WriteLine($"{formatterVar} = {outputFormatterFactoryParamName}.GetFormatter(FormatterType.TEXT);");
                            writer.CloseBlock();
                        }
                    }

                    if (originalMethod?.PagingInformation == null)
                    {
                        writer.WriteLine($"var {formatterVar} = {outputFormatterFactoryParamName}.GetFormatter({formatterTypeVal});");
                    }

                    writer.WriteLine($"await {formatterVar}.WriteOutputAsync(response, {formatterOptionsVar}, {cancellationTokenParamName});");
                }
                else
                {
                    writer.WriteLine($"if ({fileParamName} == null) {{");
                    writer.IncreaseIndent();
                    writer.WriteLine("using var reader = new StreamReader(response);");
                    writer.WriteLine("var strContent = reader.ReadToEnd();");
                    writer.WriteLine("Console.Write(strContent);");
                    writer.CloseBlock();
                    writer.WriteLine("else {");
                    writer.IncreaseIndent();
                    writer.WriteLine($"using var writeStream = {fileParamName}.OpenWrite();");
                    writer.WriteLine("await response.CopyToAsync(writeStream);");
                    writer.WriteLine($"Console.WriteLine($\"Content written to {{{fileParamName}.FullName}}.\");");
                    writer.CloseBlock();
                }
            }
        }

        private List<string> WriteExecutableCommandOptions(LanguageWriter writer, List<(string, string, CodeParameter)> parametersList)
        {
            var availableOptions = new List<string>();
            foreach (var (optionType, name, option) in parametersList)
            {
                var optionName = $"{name.ToFirstCharacterLowerCase()}Option";

                var optionBuilder = new StringBuilder("new Option");
                if (!string.IsNullOrEmpty(optionType))
                {
                    optionBuilder.Append($"<{optionType}>");
                }
                optionBuilder.Append("(\"");
                if (name.Length > 1) optionBuilder.Append('-');
                optionBuilder.Append($"-{NormalizeToOption(option.Name)}\"");
                if (option.DefaultValue != null)
                {
                    var defaultValue = optionType == "string" ? $"\"{option.DefaultValue}\"" : option.DefaultValue;
                    optionBuilder.Append($", getDefaultValue: ()=> {defaultValue}");
                }

                if (!string.IsNullOrEmpty(option.Description))
                {
                    optionBuilder.Append($", description: \"{option.Description}\"");
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
                writer.WriteLine($"command.AddOption({optionName});");
                availableOptions.Add($"{invocationContextParamName}.ParseResult.GetValueForOption({optionName})");
            }

            return availableOptions;
        }

        private static void WriteCommandDescription(CodeMethod codeElement, LanguageWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(codeElement.Description))
                writer.WriteLine($"command.Description = \"{codeElement.Description}\";");
        }

        private void WriteContainerCommand(CodeMethod codeElement, LanguageWriter writer, CodeClass parent, string name)
        {
            writer.WriteLine($"var command = new Command(\"{name}\");");
            WriteCommandDescription(codeElement, writer);

            if ((codeElement.AccessedProperty?.Type) is CodeType codeReturnType)
            {
                var targetClass = conventions.GetTypeString(codeReturnType, codeElement);

                var builderMethods = codeReturnType.TypeDefinition.GetChildElements(true).OfType<CodeMethod>()
                    .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder))
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.ReturnType.IsCollection);
                conventions.AddRequestBuilderBody(parent, targetClass, writer, prefix: "var builder = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

                foreach (var method in builderMethods)
                {
                    if (method.ReturnType.IsCollection)
                    {
                        writer.WriteLine($"foreach (var cmd in builder.{method.Name}()) {{");
                        writer.IncreaseIndent();
                        writer.WriteLine($"command.AddCommand(cmd);");
                        writer.CloseBlock();
                    }
                    else
                    {
                        writer.WriteLine($"command.AddCommand(builder.{method.Name}());");
                    }
                }
                // SubCommands
            }

            writer.WriteLine("return command;");
        }

        private void WriteUnnamedBuildCommand(CodeMethod codeElement, LanguageWriter writer, CodeClass parent, IEnumerable<CodeMethod> classMethods)
        {
            if (codeElement.OriginalMethod?.Kind == CodeMethodKind.ClientConstructor)
            {
                var commandBuilderMethods = classMethods.Where(m => m.Kind == CodeMethodKind.CommandBuilder && m != codeElement).OrderBy(m => m.Name);
                writer.WriteLine($"var command = new RootCommand();");
                WriteCommandDescription(codeElement, writer);
                foreach (var method in commandBuilderMethods)
                {
                    writer.WriteLine($"command.AddCommand({method.Name}());");
                }

                writer.WriteLine("return command;");
            }
            else if (codeElement.OriginalIndexer != null)
            {
                writer.WriteLine($"var command = new Command(\"item\");");
                var targetClass = conventions.GetTypeString(codeElement.OriginalIndexer.ReturnType, codeElement);
                var builderMethods = (codeElement.OriginalIndexer.ReturnType as CodeType).TypeDefinition.GetChildElements(true).OfType<CodeMethod>()
                    .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder))
                    .OrderBy(m => m.Name);
                conventions.AddRequestBuilderBody(parent, targetClass, writer, prefix: "var builder = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

                foreach (var method in builderMethods)
                {
                    if (method.ReturnType.IsCollection)
                    {
                        writer.WriteLine($"foreach (var cmd in builder.{method.Name}()) {{");
                        writer.IncreaseIndent();
                        writer.WriteLine($"command.AddCommand(cmd);");
                        writer.CloseBlock();
                    }
                    else
                    {
                        writer.WriteLine($"command.AddCommand(builder.{method.Name}());");
                    }
                }

                writer.WriteLine("return command;");
            }
        }

        protected virtual void WriteCommandHandlerBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            var generatorMethod = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var requestBodyParam = requestParams.requestBody;
            if (requestBodyParam != null)
            {
                var requestBodyParamType = requestBodyParam?.Type as CodeType;
                if (requestBodyParamType?.TypeDefinition is CodeClass requestBodyParamTypeDef)
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

                    requestBodyParam.Name = "model";
                }
                else if (conventions.StreamTypeName.Equals(requestBodyParamType?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var name = requestBodyParam.Name;
                    requestBodyParam.Name = "stream";
                    writer.WriteLine($"using var {requestBodyParam.Name} = {name}.OpenRead();");
                }
            }

            var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.requestConfiguration }
                                .Select(x => x?.Name).Where(x => x != null).DefaultIfEmpty().Aggregate((x, y) => $"{x}, {y}");
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
                writer.WriteLine($"{(isVoid ? string.Empty : "var pageResponse = ")}await {pagingServiceParamName}.GetPagedDataAsync((info, handler, token) => RequestAdapter.{requestMethod}(info, cancellationToken: token, responseHandler: handler), pagingData, {allParamName}, {cancellationTokenParamName});");
                writer.WriteLine($"var response = pageResponse?.Response;");
            }
            else
            {
                writer.WriteLine($"{(isVoid ? string.Empty : "var response = ")}await RequestAdapter.{requestMethod}(requestInfo, errorMapping: {errorMappingVarName}, cancellationToken: {cancellationTokenParamName});");
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
                    bool isStringParam = "string".Equals(param.Type.Name, StringComparison.OrdinalIgnoreCase) && !param.Type.IsCollection;
                    bool indentParam = true;
                    if (isStringParam)
                    {
                        writer.Write($"if (!String.IsNullOrEmpty({paramName})) ");
                        indentParam = false;
                    }

                    writer.Write($"q.QueryParameters.{param.Name.ToFirstCharacterUpperCase()} = {paramName};", indentParam);

                    writer.WriteLine();
                }
                writer.CloseBlock("});");

                foreach (var param in generatorMethod.PathQueryAndHeaderParameters.Where(p => p.IsOfKind(CodeParameterKind.Path)))
                {
                    var paramName = (string.IsNullOrEmpty(param.SerializationName) ? param.Name : param.SerializationName).SanitizeParameterNameForUrlTemplate();
                    writer.WriteLine($"requestInfo.PathParameters.Add(\"{paramName}\", {NormalizeToIdentifier(param.Name).ToFirstCharacterLowerCase()});");
                }

                foreach (var param in generatorMethod.PathQueryAndHeaderParameters.Where(p => p.IsOfKind(CodeParameterKind.Headers)))
                {
                    var paramName = string.IsNullOrEmpty(param.SerializationName) ? param.Name : param.SerializationName;
                    writer.WriteLine($"requestInfo.Headers[\"{paramName}\"] = {NormalizeToIdentifier(param.Name).ToFirstCharacterLowerCase()};");
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
            return input.ToCamelCase("-", "_", ".");
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
    }
}
