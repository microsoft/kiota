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
        private static Regex delimitedRegex = new Regex("(?<=[a-z])[-_\\.]([A-Za-z])");
        private static Regex camelCaseRegex = new Regex("(?<=[a-z])([A-Z])");
        private static Regex identifierRegex = new Regex("(?:[-_\\.]([a-zA-Z]))");

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
                //var queryStringParam = origParams.OfKind(CodeParameterKind.QueryParameter);
                //var headersParam = origParams.OfKind(CodeParameterKind.Headers);
                //var optionsParam = origParams.OfKind(CodeParameterKind.Options);
                var requestParams = new RequestParams(requestBodyParam, null, null, null);
                WriteCommandBuilderBody(codeElement, requestParams, isVoid, returnType, writer);
            }
        }

        protected void WriteCommandBuilderBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            var parent = codeElement.Parent as CodeClass;
            var classMethods = parent.Methods;
            var uppercaseRegex = new Regex("([A-Z])");
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
                    CodeType codeReturnType = (codeElement.AccessedProperty?.Type) as CodeType;

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
                var origParams = codeElement.OriginalMethod.Parameters;
                var parametersList = new List<CodeParameter>();
                parametersList.AddRange(generatorMethod.PathAndQueryParameters.Where(p => p.Name != null));
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
                    var optionBuilder = new StringBuilder("new Option(\"");
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

                var paramTypes = parametersList.Select(x => conventions.GetTypeString(x.Type, x)).Aggregate((x, y) => $"{x}, {y}");
                var paramNames = parametersList.Select(x => NormalizeToIdentifier(x.Name)).Aggregate((x, y) => $"{x}, {y}");
                var isHandlerVoid = conventions.VoidTypeName.Equals(codeElement.OriginalMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
                writer.WriteLine($"command.Handler = CommandHandler.Create<{paramTypes}>(async ({paramNames}) => {{");
                writer.IncreaseIndent();
                returnType = conventions.GetTypeString(codeElement.OriginalMethod.ReturnType, codeElement.OriginalMethod);
                WriteCommandHandlerBody(codeElement.OriginalMethod, requestParams, isHandlerVoid, returnType, writer);
                // Get request generator method. To call it + get path & query parameters see WriteRequestExecutorBody in CSharp
                writer.WriteLine("// Print request output. What if the request has no return?");
                writer.DecreaseIndent();
                writer.WriteLine("});");
                writer.WriteLine("return command;");
            }
        }

        protected virtual void WriteCommandHandlerBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var generatorMethod = (codeElement.Parent as CodeClass)
                                                .Methods
                                                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod);
            var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                                .Select(x => x?.Name).Where(x => x != null).DefaultIfEmpty().Aggregate((x, y) => $"{x}, {y}");
            writer.WriteLine($"var requestInfo = {generatorMethod?.Name}({parametersList});");
            foreach (var param in generatorMethod.PathAndQueryParameters)
            {
                if (param.IsOfKind(CodeParameterKind.Path))
                {
                    writer.WriteLine($"requestInfo.PathParameters.Add(\"{param.Name}\", {NormalizeToIdentifier(param.Name)});");
                } else if (param.IsOfKind(CodeParameterKind.QueryParameter))
                {
                    writer.WriteLine($"requestInfo.QueryParameters.Add(\"{param.Name}\", {NormalizeToIdentifier(param.Name)});");
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
