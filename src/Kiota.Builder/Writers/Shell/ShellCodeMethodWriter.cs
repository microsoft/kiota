using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Extensions;
using System.Collections.Generic;

namespace Kiota.Builder.Writers.Shell
{
    class ShellCodeMethodWriter : CodeMethodWriter
    {
        private static Regex delimitedRegex = new Regex("(?<=[a-z])[-_\\.]+([A-Za-z])", RegexOptions.Compiled);
        private static Regex camelCaseRegex = new Regex("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
        private static Regex identifierRegex = new Regex("(?:[-_\\.]([a-zA-Z]))", RegexOptions.Compiled);
        private static Regex uppercaseRegex = new Regex("([A-Z])", RegexOptions.Compiled);
        private const string consoleParamType = "IConsole";
        private const string consoleParamName = "console";
        private const string fileParamType = "FileInfo";
        private const string fileParamName = "file";
        private const string outputFormatParamType = "FormatterType";
        private const string outputFormatParamName = "output";
        private const string outputFormatterFactoryParamType = "IOutputFormatterFactory";
        private const string outputFormatterFactoryParamName = "outputFormatterFactory";

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
                if (String.IsNullOrWhiteSpace(name))
                {
                    // BuildCommand function
                    if (codeElement.OriginalMethod?.MethodKind == CodeMethodKind.ClientConstructor)
                    {
                        var commandBuilderMethods = classMethods.Where(m => m.MethodKind == CodeMethodKind.CommandBuilder && m != codeElement).OrderBy(m => m.Name);
                        writer.WriteLine($"var command = new RootCommand();");
                        writer.WriteLine($"command.Description = \"{codeElement.OriginalMethod.Description}\";");
                        foreach (var method in commandBuilderMethods)
                        {
                            writer.WriteLine($"command.AddCommand({method.Name}());");
                        }

                        writer.WriteLine("return command;");
                    }
                    else if (codeElement.OriginalIndexer != null)
                    {
                        var targetClass = conventions.GetTypeString(codeElement.OriginalIndexer.ReturnType, codeElement);
                        var builderMethods = (codeElement.OriginalIndexer.ReturnType as CodeType).TypeDefinition.GetChildElements(true).OfType<CodeMethod>()
                            .Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder))
                            .OrderBy(m => m.Name);
                        conventions.AddRequestBuilderBody(parent, targetClass, writer, prefix: "var builder = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
                        writer.WriteLine("var commands = new List<Command>();");

                        foreach (var method in builderMethods)
                        {
                            if (method.ReturnType.IsCollection)
                            {
                                writer.WriteLine($"commands.AddRange(builder.{method.Name}());");
                            }
                            else
                            {
                                writer.WriteLine($"commands.Add(builder.{method.Name}());");
                            }
                        }

                        writer.WriteLine("return commands;");
                    }
                } else
                {
                    var codeReturnType = (codeElement.AccessedProperty?.Type) as CodeType;

                    writer.WriteLine($"var command = new Command(\"{name}\");");
                    if (!string.IsNullOrEmpty(codeElement.Description) || !string.IsNullOrEmpty(codeElement?.OriginalMethod?.Description))
                        writer.WriteLine($"command.Description = \"{codeElement.Description ?? codeElement?.OriginalMethod?.Description}\";");

                    if (codeReturnType != null)
                    {
                        // Include namespace to avoid type ambiguity on similarly named classes. Currently, if we have namespaces A and A.B where both namespaces have type T,
                        // Trying to use type A.B.T in namespace A without using the fully qualified name will break the build.
                        var targetClass = string.Join(".", codeReturnType.TypeDefinition.Parent.Name, conventions.GetTypeString(codeReturnType, codeElement));
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
            } else
            {
                var generatorMethod = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
                var pathAndQueryParams = generatorMethod.PathAndQueryParameters;
                var originalMethod = codeElement.OriginalMethod;
                var origParams = originalMethod.Parameters;
                var parametersList = pathAndQueryParams?.Where(p => p.Name != null)?.ToList() ?? new List<CodeParameter>();
                if (origParams.Any(p => p.IsOfKind(CodeParameterKind.RequestBody)))
                {
                    parametersList.Add(origParams.OfKind(CodeParameterKind.RequestBody));
                }
                writer.WriteLine($"var command = new Command(\"{name}\");");
                if (codeElement.Description != null || codeElement?.OriginalMethod?.Description != null)
                    writer.WriteLine($"command.Description = \"{codeElement.Description ?? codeElement?.OriginalMethod?.Description}\";");
                writer.WriteLine("// Create options for all the parameters"); // investigate exploding query params
                // Check the possible formatting options for headers in a cli.
                // -h A=b -h
                // -h A:B,B:C
                // -h {"A": "B"}
                var availableOptions = new List<string>();
                foreach (var option in parametersList)
                {
                    var type = option.Type as CodeType;
                    var optionName = $"{NormalizeToIdentifier(option.Name)}Option";
                    var optionType = conventions.GetTypeString(option.Type, option);
                    if (option.ParameterKind == CodeParameterKind.RequestBody && type.TypeDefinition is CodeClass) optionType = "string";

                    // Binary body handling
                    if (option.ParameterKind == CodeParameterKind.RequestBody && conventions.StreamTypeName.Equals(option.Type?.Name, StringComparison.OrdinalIgnoreCase)) {
                        option.Name = "file";
                    }

                    var optionBuilder = new StringBuilder("new Option");
                    if (!String.IsNullOrEmpty(optionType))
                    {
                        optionBuilder.Append($"<{optionType}>");
                    }
                    optionBuilder.Append("(\"");
                    if (option.Name.Length > 1) optionBuilder.Append('-');
                    optionBuilder.Append($"-{NormalizeToOption(option.Name)}\"");
                    if (option.DefaultValue != null)
                    {
                        optionBuilder.Append($", getDefaultValue: ()=> {option.DefaultValue}");
                    }

                    if (!String.IsNullOrEmpty(option.Description))
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
                    availableOptions.Add(optionName);
                }

                var paramTypes = parametersList.Select(x =>
                {
                    var codeType = x.Type as CodeType;
                    if (x.ParameterKind == CodeParameterKind.RequestBody && codeType.TypeDefinition is CodeClass)
                    {
                        return "string";
                    } else if (conventions.StreamTypeName.Equals(x.Type?.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return "FileInfo";
                    }

                    return conventions.GetTypeString(x.Type, x);
                }).ToList();
                var paramNames = parametersList.Select(x => NormalizeToIdentifier(x.Name)).ToList();
                var isHandlerVoid = conventions.VoidTypeName.Equals(originalMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
                returnType = conventions.GetTypeString(originalMethod.ReturnType, originalMethod);
                if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
                {
                    var fileOptionName = "fileOption";
                    writer.WriteLine($"var {fileOptionName} = new Option<{fileParamType}>(\"--{fileParamName}\");");
                    writer.WriteLine($"command.AddOption({fileOptionName});");
                    paramTypes.Add(fileParamType);
                    paramNames.Add(fileParamName);
                    availableOptions.Add(fileOptionName);
                }

                // Add output type param
                if (!isHandlerVoid)
                {
                    var outputOptionName = "outputOption";
                    writer.WriteLine($"var {outputOptionName} = new Option<{outputFormatParamType}>(\"--{outputFormatParamName}\", () => FormatterType.JSON){{");
                    writer.IncreaseIndent();
                    writer.WriteLine("IsRequired = true");
                    writer.CloseBlock("};");
                    writer.WriteLine($"command.AddOption({outputOptionName});");
                    paramTypes.Add(outputFormatParamType);
                    paramNames.Add(outputFormatParamName);
                    availableOptions.Add(outputOptionName);
                }

                // Add output formatter factory param
                paramTypes.Add(outputFormatterFactoryParamType);
                paramNames.Add(outputFormatterFactoryParamName);

                // Add console param
                paramTypes.Add(consoleParamType);
                paramNames.Add(consoleParamName);
                var zipped = paramTypes.Zip(paramNames);
                var projected = zipped.Select((x, y) => $"{x.First} {x.Second}");
                var handlerParams = string.Join(", ", projected);
                writer.WriteLine($"command.SetHandler(async ({handlerParams}) => {{");
                writer.IncreaseIndent();
                WriteCommandHandlerBody(originalMethod, requestParams, isHandlerVoid, returnType, writer);
                // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
                if (isHandlerVoid)
                {
                    writer.WriteLine($"{consoleParamName}.WriteLine(\"Success\");");
                } else
                {
                    var type = originalMethod.ReturnType as CodeType;
                    var typeString = conventions.GetTypeString(type, originalMethod);
                    var formatterVar = "formatter";

                    writer.WriteLine($"var {formatterVar} = {outputFormatterFactoryParamName}.GetFormatter({outputFormatParamName});");
                    if (typeString != "Stream")
                    {
                        writer.WriteLine($"{formatterVar}.WriteOutput(response, {consoleParamName});");
                    } else
                    {
                        writer.WriteLine($"if ({fileParamName} == null) {{");
                        writer.IncreaseIndent();
                        writer.WriteLine($"{formatterVar}.WriteOutput(response, {consoleParamName});");
                        writer.CloseBlock();
                        writer.WriteLine("else {");
                        writer.IncreaseIndent();
                        writer.WriteLine($"using var writeStream = {fileParamName}.OpenWrite();");
                        writer.WriteLine("await response.CopyToAsync(writeStream);");
                        writer.WriteLine($"{consoleParamName}.WriteLine($\"Content written to {{{fileParamName}.FullName}}.\");");
                        writer.CloseBlock();
                    }
                }
                writer.DecreaseIndent();
                var delimiter = "";
                if (availableOptions.Any()) {
                    delimiter = ", ";
                }
                writer.WriteLine($"}}{delimiter}{string.Join(", ", availableOptions)});");
                writer.WriteLine("return command;");
            }
        }

        protected virtual void WriteCommandHandlerBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var generatorMethod = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var requestBodyParam = requestParams.requestBody;
            if (requestBodyParam != null) {
                var requestBodyParamType = requestBodyParam?.Type as CodeType;
                if (requestBodyParamType?.TypeDefinition is CodeClass)
                {
                    writer.WriteLine($"using var stream = new MemoryStream(Encoding.UTF8.GetBytes({requestBodyParam.Name}));");
                    writer.WriteLine("var parseNode = ParseNodeFactoryRegistry.DefaultInstance.GetRootParseNode(\"application/json\", stream);");

                    var typeString = conventions.GetTypeString(requestBodyParamType, requestBodyParam, false);

                    if (requestBodyParamType.IsCollection)
                    {
                        writer.WriteLine($"var model = parseNode.GetCollectionOfObjectValues<{typeString}>();");
                    } else
                    {
                        writer.WriteLine($"var model = parseNode.GetObjectValue<{typeString}>();");
                    }

                    requestBodyParam.Name = "model";
                } else if (conventions.StreamTypeName.Equals(requestBodyParamType?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var name = requestBodyParam.Name;
                    requestBodyParam.Name = "stream";
                    writer.WriteLine($"using var {requestBodyParam.Name} = {name}.OpenRead();");
                }
            }
            
            var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                                .Select(x => x?.Name).Where(x => x != null).DefaultIfEmpty().Aggregate((x, y) => $"{x}, {y}");
            var separator = string.IsNullOrWhiteSpace(parametersList) ? "" : ", ";
            writer.WriteLine($"var requestInfo = {generatorMethod?.Name}({parametersList}{separator}q => {{");
            if (generatorMethod?.PathAndQueryParameters != null)
            {
                writer.IncreaseIndent();
                foreach (var param in generatorMethod.PathAndQueryParameters.Where(p => p.IsOfKind(CodeParameterKind.QueryParameter)))
                {
                    var paramName = NormalizeToIdentifier(param.Name);
                    bool isStringParam = param.Type.Name?.ToLower() == "string" && !param.Type.IsCollection;
                    bool indentParam = true;
                    if (isStringParam)
                    {
                        writer.Write($"if (!String.IsNullOrEmpty({paramName})) ");
                        indentParam = false;
                    }

                    writer.Write($"q.{param.Name.ToFirstCharacterUpperCase()} = {paramName};", indentParam);

                    writer.WriteLine();
                }
                writer.CloseBlock("});");

                foreach (var paramName in generatorMethod.PathAndQueryParameters.Where(p => p.IsOfKind(CodeParameterKind.PathParameters)).Select(p => p.Name))
                {
                    writer.WriteLine($"requestInfo.PathParameters.Add(\"{paramName}\", {NormalizeToIdentifier(paramName)});");
                }
            }
            else
            {
                writer.WriteLine("});");
            }

            var requestMethod = "SendPrimitiveAsync<Stream>";
            if (isVoid)
            {
                requestMethod = "SendNoContentAsync";
            }

            writer.WriteLine($"{(isVoid ? string.Empty : "var response = ")}await RequestAdapter.{requestMethod}(requestInfo);");
        }

        /// <summary>
        /// Converts delimited string into camel case for use as identifiers
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string NormalizeToIdentifier(string input)
        {
            return identifierRegex.Replace(input, m => m.Groups[1].Value.ToUpper());
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
