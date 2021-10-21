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

        protected override void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

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

            CodeElement element = codeElement;

            //while (element != null)
            //{
            //    foreach (CodeParameter p in element.)
            //    {

            //    }

            //    element = element.Parent;
            //}
            writer.WriteLine("// Create options for all the parameters");
        }
    }
}
