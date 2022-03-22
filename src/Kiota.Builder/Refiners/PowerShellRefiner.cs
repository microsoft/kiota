using System.Linq;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Refiners
{
    public class PowerShellRefiner : CSharpRefiner
    {
        public PowerShellRefiner(GenerationConfiguration configuration) : base(configuration) { }

        // Rename request builder classes to PS cmdlets i.e. MessageRequestBuilder -> UserMessage.

        // Move OpenAPI operations to separate classes i.e. MessageRequestBuilder (Get, POST) -> GetMessageRequest, NewMessageRequest.

        public override void Refine(CodeNamespace generatedCode)
        {
            base.Refine(generatedCode);
            MoveOperationsToClasses(generatedCode);
        }

        private void MoveOperationsToClasses(CodeElement currentElement)
        {
            if (currentElement is CodeClass currentClass &&
                !string.IsNullOrEmpty(currentClass.Name) &&
                currentClass.Kind == CodeClassKind.RequestBuilder &&
                currentClass.Parent is CodeNamespace parentNamespace)
            {
                parentNamespace.RemoveChildElement(currentElement);
                var requestExecutors = currentClass.GetMethodsOffKind(CodeMethodKind.RequestExecutor);
                var requestGenerators = currentClass.GetMethodsOffKind(CodeMethodKind.RequestGenerator);
                //"GetQueryParameters"
                var queryParameters = currentClass.FindChildByName<CodeClass>("GetQueryParameters");
                if (queryParameters != null)
                {

                    foreach (var requestExecutor in requestExecutors)
                    {
                        var newClass = new CodeClass
                        {
                            //TODO: Map HTTP verbs to PS verbs.
                            Name = $"{currentClass.Name}{requestExecutor.Name}",
                            Kind = CodeClassKind.RequestBuilder,
                            Parent = parentNamespace,
                            Description = requestExecutor.Description
                        };
                        var requestGenerator = requestGenerators.Where(g => g.HttpMethod == requestExecutor.HttpMethod).FirstOrDefault();
                        newClass.AddMethod(requestGenerator);
                        newClass.AddMethod(requestExecutor);
                        newClass.AddProperty(currentClass.Properties.ToArray());
                        newClass.AddProperty(queryParameters.Properties.ToArray());

                        var baseUrlProperty = new CodeProperty
                        {
                            Name = "BaseUrl",
                            Description = "The base URL of the API.",
                            DefaultValue = requestExecutor.BaseUrl,//TODO: Fetch base URI.
                            Access = AccessModifier.Private,
                            Kind = CodePropertyKind.Custom,
                            Type = new CodeType
                            {
                                Name = "string",
                                IsNullable = false,
                                IsExternal = true,
                            }
                        };
                        newClass.AddProperty(baseUrlProperty);

                        // RequestAdapter prop is needed.

                        //var newMethod = new CodeMemberMethod();
                        //newMethod.Name = operation.Name;
                        //newMethod.ReturnType = operation.ReturnType;
                        //newMethod.Attributes = operation.Attributes;
                        //newMethod.Parameters.AddRange(operation.Parameters);

                        //newClass.Members.Add(newMethod);

                        //currentClass.Members.Remove(operation);
                        //currentClass.Members.Add(newClass);
                        parentNamespace.AddClass(newClass);
                    }
                }
            }
            CrawlTree(currentElement, MoveOperationsToClasses);
        }
    }
}
