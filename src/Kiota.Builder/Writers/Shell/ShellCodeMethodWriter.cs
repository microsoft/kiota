using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kiota.Builder.Writers.CSharp;

namespace Kiota.Builder.Writers.Shell
{
    class ShellCodeMethodWriter : CodeMethodWriter
    {
        public ShellCodeMethodWriter(CSharpConventionService conventionService) : base(conventionService)
        {
        }

        protected override void HandleMethodKind(CodeMethod codeElement, LanguageWriter writer, bool inherits, CodeClass parentClass, bool isVoid)
        {
            base.HandleMethodKind(codeElement, writer, inherits, parentClass, isVoid);
            if (codeElement.MethodKind == CodeMethodKind.CommandBuilder)
            {
                var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
                var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
                var queryStringParam = codeElement.Parameters.OfKind(CodeParameterKind.QueryParameter);
                var headersParam = codeElement.Parameters.OfKind(CodeParameterKind.Headers);
                var optionsParam = codeElement.Parameters.OfKind(CodeParameterKind.Options);
                var requestParams = new RequestParams(requestBodyParam, queryStringParam, headersParam, optionsParam);
                WriteCommandBuilderBody(codeElement, requestParams, isVoid, returnType, writer);
            }
        }

        protected void WriteCommandBuilderBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            var parent = codeElement.Parent as CodeClass;
            var classMethods = parent.Methods;
            var nameRegex = new Regex("(?:[Bb]uild|[Cc]ommand)"); // Use convention for command builder
            var uppercaseRegex = new Regex("([A-Z])");
            var name = nameRegex.Replace(codeElement.Name, "");
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

                        writer.WriteLine("var allCommands = new List<Command>();");
                        foreach (var method in builderMethods)
                        {
                            if (method.ReturnType.IsCollection)
                            {
                                writer.WriteLine($"allCommands.AddRange(builder.{method.Name}());");
                            } else
                            {
                                writer.WriteLine($"allCommands.Add(builder.{method.Name}());");
                            }
                        }
                        writer.WriteLine("foreach (var cmd in allCommands) {");
                        writer.IncreaseIndent();
                        writer.WriteLine("command.AddCommand(cmd);");
                        writer.DecreaseIndent();
                        writer.WriteLine("}");
                        // SubCommands
                    }

                    writer.WriteLine("return command;");
                }
            } else
            {
                var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
                var executorMethod = classMethods.FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestExecutor) && x.HttpMethod == codeElement.HttpMethod);
                var origParams = codeElement.OriginalMethod.Parameters;
                var pathAndQueryParams = codeElement.OriginalMethod.PathAndQueryParameters; // Investigate why this is null
                var parametersList = new CodeParameter[] {
                    origParams.OfKind(CodeParameterKind.RequestBody),
                    origParams.OfKind(CodeParameterKind.QueryParameter),
                    origParams.OfKind(CodeParameterKind.Headers),
                    origParams.OfKind(CodeParameterKind.Options)
                }.Where(x => x?.Name != null);
                writer.WriteLine($"var command = new Command(\"{name}\");");
                writer.WriteLine("// Create options for all the parameters"); // investigate exploding query params

                foreach (var option in origParams)
                {
                    if (option.ParameterKind == CodeParameterKind.ResponseHandler)
                    {
                        continue;
                    }
                    var optionBuilder = new StringBuilder("new Option(");
                    optionBuilder.Append($"\"{option.Name}\"");
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
                var paramNames = parametersList.Select(x => x.Name).Aggregate((x, y) => $"{x}, {y}");
                var isExecutorVoid = conventions.VoidTypeName.Equals(executorMethod.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
                writer.WriteLine($"command.Handler = CommandHandler.Create<{paramTypes}>(async ({paramNames}) => {{");
                writer.IncreaseIndent();
                writer.WriteLine($"{(isExecutorVoid ? String.Empty : "var result = ")}await {executorMethod.Name}({paramNames});");
                writer.WriteLine("// Print request output. What if the request has no return?");
                writer.DecreaseIndent();
                writer.WriteLine("});");
                writer.WriteLine("return command;");
            }
        }
    }
}
