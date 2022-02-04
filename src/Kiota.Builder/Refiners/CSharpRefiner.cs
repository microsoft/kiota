using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class CSharpRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public CSharpRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddRawUrlConstructorOverload(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode, false);
            AddParsableInheritanceForModelClasses(generatedCode, "IParsable");
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            /* Exclude the following as their names will be capitalized making the change unnecessary in this case sensitive language
             * code classes, class declarations, property names, using declarations, namespace names
             * Exclude CodeMethod as the return type will also be capitalized (excluding the CodeType is not enough since this is evaluated at the code method level)
            */
            ReplaceReservedNames(
                generatedCode,
                new CSharpReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}",
                new HashSet<Type>{ typeof(CodeClass), typeof(CodeClass.Declaration), typeof(CodeProperty), typeof(CodeUsing), typeof(CodeNamespace), typeof(CodeMethod) }
            ); 
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
            AddSerializationModulesImport(generatedCode);
            AddParentClassToErrorClasses(
                generatedCode,
                "ApiException",
                "Microsoft.Kiota.Abstractions"
            );
        }
        private static void DisambiguatePropertiesWithClassNames(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var sameNameProperty = currentClass.Properties
                                                .FirstOrDefault(x => x.Name.Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase));
                if(sameNameProperty != null) {
                    currentClass.RemoveChildElement(sameNameProperty);
                    sameNameProperty.SerializationName ??= sameNameProperty.Name;
                    sameNameProperty.Name = $"{sameNameProperty.Name}_prop";
                    currentClass.AddProperty(sameNameProperty);
                }
            }
            CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
        }
        private static void MakeEnumPropertiesNullable(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
                currentClass.Properties
                            .Where(x => x.Type is CodeType propType && propType.TypeDefinition is CodeEnum)
                            .ToList()
                            .ForEach(x => x.Type.IsNullable = true);
            CrawlTree(currentElement, MakeEnumPropertiesNullable);
        }
        
        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "Microsoft.Kiota.Abstractions", "IRequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "Microsoft.Kiota.Abstractions", "Method", "RequestInformation", "IRequestOption"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft.Kiota.Abstractions", "IResponseHandler"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "Microsoft.Kiota.Abstractions.Serialization", "ISerializationWriter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "Microsoft.Kiota.Abstractions.Serialization", "IParseNode"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "Microsoft.Kiota.Abstractions.Serialization", "IParsable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft.Kiota.Abstractions.Serialization", "IParsable"),
            new (x => x is CodeClass || x is CodeEnum,
                "System", "String"),
            new (x => x is CodeClass,
                "System.Collections.Generic", "List", "Dictionary"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
                "System.IO", "Stream"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "System.Threading", "CancellationToken"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.Threading.Tasks", "Task"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
                "System.Linq", "Enumerable"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                        method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "Microsoft.Kiota.Abstractions.Store",  "IBackingStoreFactory", "IBackingStoreFactorySingleton"),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "Microsoft.Kiota.Abstractions.Store",  "IBackingStore", "IBackedModel", "BackingStoreFactorySingleton" ),
        };
        private static void CapitalizeNamespacesFirstLetters(CodeElement current) {
            if(current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
        private static void AddAsyncSuffix(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
                currentMethod.Name += "Async";
            CrawlTree(currentElement, AddAsyncSuffix);
        }
        private static void CorrectPropertyType(CodeProperty currentProperty)
        {
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        }
        private static void CorrectMethodType(CodeMethod currentMethod)
        {
            CorrectDateTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                    .Select(x => x.Type)
                                                    .Union(new CodeTypeBase[] { currentMethod.ReturnType })
                                                    .ToArray());
        }
        private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "DateOnly",("Date", new CodeUsing
                    {
                        Name = "Date",
                        Declaration = new CodeType
                        {
                            Name = "Microsoft.Kiota.Abstractions",
                            IsExternal = true,
                        },
                    })
            },
            {
                "TimeOnly",("Time", new CodeUsing
                    {
                        Name = "Time",
                        Declaration = new CodeType
                        {
                            Name = "Microsoft.Kiota.Abstractions",
                            IsExternal = true,
                        },
                    })
            },
        };
    }
}
