using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (codeElement.HttpMethod == null)
            {
                // Build method
                // Puts together the BuildXXCommand objects. Needs a nav property name e.g. users
                // Command("users") -> Command("get")
            } else
            {
                var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
                var generatorMethodName = (codeElement.Parent as CodeClass)
                                                    .Methods
                                                    .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                                    ?.Name;
                var parametersList = new CodeParameter[] { requestParams.requestBody, requestParams.queryString, requestParams.headers, requestParams.options }
                                    .Select(x => x?.Name).Where(x => x != null).Aggregate((x, y) => $"{x}, {y}");
                writer.WriteLine($"var command = new Command(\"{codeElement.HttpMethod.ToString().ToLower()}\") {{");
                writer.IncreaseIndent();
                writer.WriteLine($"Handler = CommandHandler.Create<>(async () => {{");
                writer.IncreaseIndent();
                writer.WriteLine($"var requestInfo = {generatorMethodName}({parametersList});");
                writer.WriteLine($"{(isVoid ? string.Empty : "return ")}await HttpCore.{GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnType)}(requestInfo, responseHandler);");
                writer.DecreaseIndent();
                writer.WriteLine("})");
                writer.DecreaseIndent();
                writer.WriteLine("};");
                writer.WriteLine("// Create options for all the parameters"); // investigate exploding query params

                foreach (var option in codeElement.Parameters)
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
                    writer.WriteLine($"// {option.Type.Name}"); //GetTypeString
                }
            }
        }
    }
}
