using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class PhpRefiner: CommonLanguageRefiner
    {
        public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }
        
        public override void Refine(CodeNamespace generatedCode)
        {
            //AddInnerClasses(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            MakeModelPropertiesNullable(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddGetterAndSetterMethods(generatedCode,new HashSet<CodePropertyKind>()
            {
                CodePropertyKind.Custom,
                CodePropertyKind.AdditionalData,
                CodePropertyKind.BackingStore
            }, _configuration.UsesBackingStore, true);
            ReplaceReservedNames(generatedCode, new PhpReservedNamesProvider(), reservedWord => $"Graph{reservedWord.ToFirstCharacterUpperCase()}");
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "StreamInterface", "Psr\\Http\\Message", true);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            CorrectCoreType(generatedCode, null, CorrectPropertyType);
           
        }
        
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "Microsoft\\Kiota\\Abstractions", "RequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "Microsoft\\Kiota\\Abstractions", "HttpMethod", "RequestInformation", "MiddlewareOption"),
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
        };
        
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.AddImplements(new CodeType {
                    IsExternal = true,
                    Name = $"Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }

        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.RequestAdapter)) {
                currentProperty.Type.Name = "RequestAdapter";
                currentProperty.Type.IsNullable = true;
            }
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..];
            else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "array";
                currentProperty.DefaultValue = "[]";
            } else if(currentProperty.IsOfKind(CodePropertyKind.UrlTemplate)) {
                currentProperty.Type.IsNullable = true;
            } else if(currentProperty.IsOfKind(CodePropertyKind.PathParameters)) {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "array";
                if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = "[]";
            }
        }
    }
}
