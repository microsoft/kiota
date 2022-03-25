using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Kiota.Builder.Extensions;
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
            MoveOperationsToClasses(generatedCode);
            AddDefaultImports(generatedCode, GetPowerShellImports());
            AddDefaultImports(generatedCode, powerShellUsingEvaluators);
            AddAsyncSuffix(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            /* Exclude the following as their names will be capitalized making the change unnecessary in this case sensitive language
             * code classes, class declarations, property names, using declarations, namespace names
             * Exclude CodeMethod as the return type will also be capitalized (excluding the CodeType is not enough since this is evaluated at the code method level)
            */
            ReplaceReservedNames(
                generatedCode,
                new PowerShellReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}",
                new HashSet<Type> { typeof(CodeClass), typeof(ClassDeclaration), typeof(CodeProperty), typeof(CodeUsing), typeof(CodeNamespace), typeof(CodeMethod) }
            );
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
                foreach (var requestExecutor in requestExecutors)
                {
                    string entityName;
                    if (parentNamespace.IsItemNamespace)
                    {
                        // TODO: Add parent and nav prop (item) indexer as cmdlet parameter.
                        entityName = currentClass.Name.Replace("ItemRequestBuilder", string.Empty);
                    }
                    else
                    {
                        // TODO: Add parent indexer as cmdlet parameter.
                        entityName = currentClass.Name.Replace("RequestBuilder", string.Empty);
                    }
                    CodeClass cmdletClass = GetCmdletClass(requestExecutor, entityName, parentNamespace);

                    var requiredProperties = currentClass.GetPropertiesOfKind(CodePropertyKind.RequestAdapter, CodePropertyKind.UrlTemplate, CodePropertyKind.PathParameters, CodePropertyKind.QueryParameter, CodePropertyKind.AdditionalData);
                    cmdletClass.AddProperty(requiredProperties.ToArray());

                    var requestGenerator = requestGenerators.Where(g => g.HttpMethod == requestExecutor.HttpMethod).FirstOrDefault();
                    cmdletClass.AddMethod(requestGenerator);
                    cmdletClass.AddMethod(requestExecutor);
                    cmdletClass.AddMethod(GetCmdletMethods());

                    parentNamespace.AddClass(cmdletClass);
                }
            }
            CrawlTree(currentElement, MoveOperationsToClasses);
        }

        private CodeClass GetCmdletClass(CodeMethod currentMethod, string entityName, CodeNamespace parentNamespace)
        {
            string className = GetClassName(currentMethod.HttpMethod, entityName, parentNamespace);
            // TODO: Add *_{UniqueParameterSetName} to class namme.
            var newClass = new CodeClass
            {
                Name = className,
                Kind = CodeClassKind.RequestBuilder,
                Parent = parentNamespace,
                Description = currentMethod.Description,
            };
            newClass.StartBlock.AddImplements(new CodeType { Name = "PSCmdlet", IsExternal = true });

            //TODO: Add PS's Cmdlet & OutputType C# annotation.
            return newClass;
        }

        private CodeMethod[] GetCmdletMethods()
        {
            var voidReturnType = new CodeType { Name = "void" };
            var processRecordAsync = GetCodeMethod("ProcessRecordAsync", voidReturnType);
            processRecordAsync.IsAsync = true;
            processRecordAsync.IsOverride = false;

            return new[] {
                GetCodeMethod("BeginProcessing", voidReturnType),
                GetCodeMethod("EndProcessing", voidReturnType),
                GetCodeMethod("StopProcessing", voidReturnType),
                GetCodeMethod("ProcessRecord", voidReturnType),
                processRecordAsync
            };
        }

        private CodeMethod GetCodeMethod(string name, CodeType returnType)
        {
            return new CodeMethod
            {
                Name = name,
                Access = AccessModifier.Protected,
                Kind = CodeMethodKind.Custom,
                ReturnType = returnType,
                IsAsync = false,
                IsOverride = true
            };
        }

        private CodeProperty GetPropertyFromIndexer(CodeIndexer indexer)
        {
            return new CodeProperty
            {
                Name = indexer.Name.Replace("-indexer", "Id"),
                Description = indexer.Description,
                Access = AccessModifier.Public,
                Kind = CodePropertyKind.Custom,
                Type = indexer.IndexType
            };
        }

        private string GetClassName(HttpMethod? httpMethod, string entityName, CodeNamespace parentNamespace)
        {
            if (!httpMethod.HasValue)
                throw new ArgumentNullException(nameof(httpMethod));
            var namespaceName = parentNamespace.Name.Replace($"{_configuration.ClientNamespaceName}.", string.Empty, StringComparison.InvariantCultureIgnoreCase).Replace(".Item", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            string pathSegmentNoun = string.Empty;
            var namespaceSegments = namespaceName.Split(".");
            int namespaceSegmentDepth = namespaceSegments.Length;
            if (namespaceSegmentDepth > 2)
            {
                string previousPathNoun = namespaceSegments[namespaceSegmentDepth - 2].Singularize();
                if (previousPathNoun.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) &&
                    previousPathNoun.Equals("value", StringComparison.InvariantCultureIgnoreCase))
                    previousPathNoun = namespaceSegments[namespaceSegmentDepth - 3].Singularize();
                pathSegmentNoun = previousPathNoun.ToFirstCharacterUpperCase(); ;
            }

            string entityNoun = entityName.ToFirstCharacterUpperCase();
            string verb = GetPowerShellVerb(httpMethod.Value);
            var commandName = $"{verb}{pathSegmentNoun}{entityNoun}".SplitAndSingularizePascalCase().Distinct().Aggregate((x, y) => $"{x}{y}");
            return commandName;
        }

        private string GetPowerShellVerb(HttpMethod httpMethod)
        {
            return httpMethod switch
            {
                HttpMethod.Get => "Get",
                HttpMethod.Post => "New",
                HttpMethod.Put => "Set",
                HttpMethod.Patch => "Update",
                HttpMethod.Delete => "Remove",
                _ => "Invoke",
            };
        }

        private static readonly AdditionalUsingEvaluator[] powerShellUsingEvaluators = new AdditionalUsingEvaluator[] {
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.Management.Automation", "PSCmdlet", "Cmdlet", "InvocationInfo", "SwitchParameter")
        };

        private IEnumerable<AdditionalUsingEvaluator> GetPowerShellImports()
        {
            var imports = new List<AdditionalUsingEvaluator>(defaultUsingEvaluators);
            imports.AddRange(powerShellUsingEvaluators);
            return imports;
        }
    }
}
