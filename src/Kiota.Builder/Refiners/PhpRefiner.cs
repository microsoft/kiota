using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class PhpRefiner: CommonLanguageRefiner
    {
        public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }
        
        public override void Refine(CodeNamespace generatedCode)
        {
            ConvertUnionTypesToWrapper(generatedCode, false);
            AddConstructorsForDefaultValues(generatedCode, true);
            RemoveCancellationTokenParameter(generatedCode);
            CorrectParameterType(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            MakeModelPropertiesNullable(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            AddGetterAndSetterMethods(generatedCode,new()
            {
                CodePropertyKind.Custom,
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore
            }, _configuration.UsesBackingStore, true);
            ReplaceReservedNames(generatedCode, new PhpReservedNamesProvider(), reservedWord => $"Graph{reservedWord.ToFirstCharacterUpperCase()}");
            AddParsableInheritanceForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "StreamInterface", "Psr\\Http\\Message", true);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
        }
        
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "Microsoft\\Kiota\\Abstractions", "RequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "Microsoft\\Kiota\\Abstractions", "HttpMethod", "RequestInformation", "RequestOption"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft\\Kiota\\Abstractions", "ResponseHandler"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "Microsoft\\Kiota\\Abstractions\\Serialization", "SerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "Microsoft\\Kiota\\Abstractions\\Serialization", "ParseNode"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft\\Kiota\\Abstractions\\Serialization", "Parsable"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "Microsoft\\Kiota\\Abstractions\\Serialization", "Parsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                      method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "Microsoft\\Kiota\\Abstractions\\Store", "BackingStoreFactory", "BackingStoreFactorySingleton"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "Microsoft\\Kiota\\Abstractions\\Store", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor), "Http\\Promise", "Promise", "RejectedPromise"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor), "", "Exception"),
            new (x => x is CodeEnum, "Microsoft\\Kiota\\Abstractions\\", "Enum")
        };
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter)) {
                currentProperty.Type.Name = "RequestAdapter";
                currentProperty.Type.IsNullable = false;
            }
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..];
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "array";
                currentProperty.Type.IsNullable = false;
                currentProperty.DefaultValue = "[]";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
            } else if(currentProperty.IsOfKind(CodePropertyKind.UrlTemplate)) {
                currentProperty.Type.IsNullable = false;
            } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "array";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = "[]";
            } else if (currentProperty.IsOfKind(CodePropertyKind.RequestBuilder))
            {
                currentProperty.Type.Name = currentProperty.Type.Name.ToFirstCharacterUpperCase();
            }
        }

        private static void CorrectMethodType(CodeMethod method)
        {
            if (method.IsOfKind(CodeMethodKind.Deserializer))
            {
                method.ReturnType.Name = "array";
            } else if (method.IsOfKind(CodeMethodKind.RequestExecutor))
            {
                method.ReturnType = new CodeType() {Name = "Promise", IsExternal = true, IsNullable = false};
            }
        }
        private static void RemoveCancellationTokenParameter(CodeElement codeElement)
        {
            var currentMethod = codeElement as CodeMethod;
            currentMethod?.RemoveParametersByKind(CodeParameterKind.Cancellation);
            CrawlTree(codeElement, RemoveCancellationTokenParameter);
        }

        private static void CorrectParameterType(CodeElement codeElement)
        {
            var currentMethod = codeElement as CodeMethod;
            var parameters = currentMethod?.Parameters;
            parameters?.Where(x => x.IsOfKind(CodeParameterKind.Options, CodeParameterKind.Headers)).ToList().ForEach(x =>
            {
                x.Type.Name = "array";
            });
            CrawlTree(codeElement, CorrectParameterType);
        }
    }
}
