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
        private static Regex delimitedRegex = new Regex("(?<=[a-z])[-_\\.]([A-Za-z])", RegexOptions.Compiled);
        private static Regex camelCaseRegex = new Regex("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
        private static Regex identifierRegex = new Regex("(?:[-_\\.]([a-zA-Z]))", RegexOptions.Compiled);
        private static Regex uppercaseRegex = new Regex("([A-Z])", RegexOptions.Compiled);

        public ShellCodeMethodWriter(CSharpConventionService conventionService) : base(conventionService)
        {
        }

        protected override void HandleMethodKind(CodeMethod codeElement, LanguageWriter writer, bool inherits, CodeClass parentClass, bool isVoid)
        {
            base.HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
            if (codeElement.MethodKind == CodeMethodKind.CommandBuilder)
            {
                var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
                var origParams = codeElement.OriginalMethod?.Parameters ?? codeElement.Parameters;
                var requestBodyParam = origParams.OfKind(CodeParameterKind.RequestBody);
                var requestParams = new RequestParams(requestBodyParam, null, null, null);
                WriteCommandBuilderBody(codeElement, requestParams, isVoid, returnType, writer);
            }
        }

        protected void WriteCommandBuilderBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
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
                        var commandBuilderMethods = classMethods.Where(m => m.MethodKind == CodeMethodKind.CommandBuilder && m != codeElement);
                        writer.WriteLine($"var command = new RootCommand();");
                        foreach (var method in commandBuilderMethods)
                        {
                            writer.WriteLine($"command.AddCommand({method.Name}());");
                        }

                        writer.WriteLine("return command;");
                    }
                    else if (codeElement.OriginalIndexer != null)
                    {
                        var targetClass = conventions.GetTypeString(codeElement.OriginalIndexer.ReturnType, codeElement);
                        var builderMethods = (codeElement.OriginalIndexer.ReturnType as CodeType).TypeDefinition.GetChildElements(true).OfType<CodeMethod>().Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder)).ToList();
                        conventions.AddRequestBuilderBody(parent, targetClass, writer, prefix: "var builder = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));
                        writer.WriteLine("return new Command[] { ");
                        writer.IncreaseIndent();
                        for (int i = 0; i < builderMethods.Count; i++)
                        {
                            writer.WriteLine($"builder.{builderMethods[i].Name}(),");
                        }
                        writer.DecreaseIndent();
                        writer.WriteLine("};");
                    }
                } else
                {
                    var codeReturnType = (codeElement.AccessedProperty?.Type) as CodeType;

                    writer.WriteLine($"var command = new Command(\"{name}\");");

                    if (codeReturnType != null)
                    {
                        var targetClass = conventions.GetTypeString(codeReturnType, codeElement);
                        var builderMethods = codeReturnType.TypeDefinition.GetChildElements(true).OfType<CodeMethod>().Where(m => m.IsOfKind(CodeMethodKind.CommandBuilder));
                        conventions.AddRequestBuilderBody(parent, targetClass, writer, prefix: "var builder = ", pathParameters: codeElement.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path)));

                        foreach (var method in builderMethods)
                        {
                            if (method.ReturnType.IsCollection)
                            {
                                writer.WriteLine($"foreach (var cmd in builder.{method.Name}()) {{");
                                writer.IncreaseIndent();
                                writer.WriteLine("command.AddCommand(cmd);");
                                writer.DecreaseIndent();
                                writer.WriteLine("}");
                            } else
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
                var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
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
                writer.WriteLine("// Create options for all the parameters"); // investigate exploding query params
                // Check the possible formatting options for headers in a cli.
                // -h A=b -h
                // -h A:B,B:C
                // -h {"A": "B"}

                foreach (var option in parametersList)
                {
                    var type = option.Type as CodeType;
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

                    optionBuilder.Append(')');
                    writer.WriteLine($"command.AddOption({optionBuilder});");
                }

                var paramTypes = parametersList.Any() ? parametersList.Select(x =>
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
                }).Aggregate((x, y) => $"{x}, {y}") : "";
                var paramNames = parametersList.Any() ? parametersList.Select(x => NormalizeToIdentifier(x.Name)).Aggregate((x, y) => $"{x}, {y}") : "";
                var isHandlerVoid = conventions.VoidTypeName.Equals(originalMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
                returnType = conventions.GetTypeString(originalMethod.ReturnType, originalMethod);
                if (conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteLine("command.AddOption(new Option<FileInfo>(\"--output\"));");
                    paramTypes = String.Join(paramTypes, "FileInfo");
                    paramNames = String.Join(paramNames, "output");
                }
                var genericParameter = paramTypes.Length > 0 ? String.Join("", "<", paramTypes, ">") : "";
                writer.WriteLine($"command.Handler = CommandHandler.Create{genericParameter}(async ({paramNames}) => {{");
                writer.IncreaseIndent();
                WriteCommandHandlerBody(originalMethod, requestParams, isHandlerVoid, returnType, writer);
                // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
                writer.WriteLine("// Print request output. What if the request has no return?");
                if (isHandlerVoid)
                {
                    writer.WriteLine("Console.WriteLine(\"Success\");");
                } else
                {
                    var type = originalMethod.ReturnType as CodeType;
                    var typeString = conventions.GetTypeString(type, originalMethod);
                    var contentType = originalMethod.ContentType ?? "application/json";
                    if (typeString != "Stream")
                        writer.WriteLine($"using var serializer = RequestAdapter.SerializationWriterFactory.GetSerializationWriter(\"{contentType}\");");

                    if (type.TypeDefinition is CodeEnum)
                    {
                        if (type.IsCollection)
                            writer.WriteLine($"serializer.WriteCollectionOfEnumValues(null, result);");
                        else
                            writer.WriteLine($"serializer.WriteEnumValue(null, result);");
                    }
                    else if (conventions.IsPrimitiveType(typeString))
                    {
                        if (type.IsCollection)
                            writer.WriteLine($"serializer.WriteCollectionOfPrimitiveValues(null, result);");
                        else
                            writer.WriteLine($"serializer.Write{typeString.ToFirstCharacterUpperCase().Replace("?", "")}Value(null, result);");
                    }
                    else
                    {
                        if (type.IsCollection)
                            writer.WriteLine($"serializer.WriteCollectionOfObjectValues(null, result);");
                        else if (typeString == "Stream") { }
                        else
                            writer.WriteLine($"serializer.WriteObjectValue(null, result);");
                    }

                    if (typeString != "Stream")
                    {
                        writer.WriteLine("using var content = serializer.GetSerializedContent();");
                        WriteResponseToConsole(writer, "content");
                    } else
                    {
                        writer.WriteLine("if (output == null) {");
                        writer.IncreaseIndent();
                        WriteResponseToConsole(writer, "result");
                        writer.CloseBlock();
                        writer.WriteLine("else {");
                        writer.IncreaseIndent();
                        writer.WriteLine("using var stream = output.OpenWrite();");
                        writer.WriteLine("await result.CopyToAsync(stream);");
                        writer.WriteLine("Console.WriteLine($\"Content written to {output.FullName}.\");");
                        writer.CloseBlock();
                    }

                    // Assume string content as stream here
                    
                }
                writer.DecreaseIndent();
                writer.WriteLine("});");
                writer.WriteLine("return command;");
            }
        }

        private void WriteResponseToConsole(LanguageWriter writer, string argName)
        {
            writer.WriteLine($"using var reader = new StreamReader({argName});");
            writer.WriteLine("var strContent = await reader.ReadToEndAsync();");
            writer.WriteLine("Console.Write(strContent + \"\\n\");");
        }

        protected virtual void WriteCommandHandlerBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var generatorMethod = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var requestBodyParam = requestParams.requestBody;
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
            var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                                .Select(x => x?.Name).Where(x => x != null).DefaultIfEmpty().Aggregate((x, y) => $"{x}, {y}");
            writer.WriteLine($"var requestInfo = {generatorMethod?.Name}({parametersList});");
            if (generatorMethod.PathAndQueryParameters != null)
            {
                foreach (var param in generatorMethod.PathAndQueryParameters)
                {
                    var paramName = NormalizeToIdentifier(param.Name);
                    bool isStringParam = param.Type.Name?.ToLower() == "string";
                    if (isStringParam) writer.Write($"if (!String.IsNullOrEmpty({paramName})) ");
                    if (param.IsOfKind(CodeParameterKind.Path))
                    {
                        writer.Write($"requestInfo.PathParameters.Add(\"{param.Name}\", {paramName});", !isStringParam);
                    }
                    else if (param.IsOfKind(CodeParameterKind.QueryParameter))
                    {
                        writer.Write($"requestInfo.QueryParameters.Add(\"{param.Name}\", {paramName});", !isStringParam);
                    }

                    writer.WriteLine();
                }
            }


            writer.WriteLine($"{(isVoid ? string.Empty : "var result = ")}await RequestAdapter.{GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnType)}(requestInfo);");
        }

        /// <summary>
        /// Converts delimited string into camel case for use as identifiers
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string NormalizeToIdentifier(string input)
        {
            return identifierRegex.Replace(input, m => m.Groups[1].Value.ToUpper());
        }

        /// <summary>
        /// Converts camel-case or delimited string to '-' delimited string for use as a command option
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string NormalizeToOption(string input)
        {
            var result = input;
            result = camelCaseRegex.Replace(input, "-$1");
            // 2 passes for cases like "singleValueLegacyExtendedProperty_id"
            result = delimitedRegex.Replace(input, "-$1");

            return result.ToLower();
        }
    }
}
