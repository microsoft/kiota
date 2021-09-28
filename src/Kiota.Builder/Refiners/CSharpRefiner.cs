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
            AddDefaultImports(generatedCode, defaultNamespaces);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode, false);
            AddParsableInheritanceForModelClasses(generatedCode);
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            // Exclude code classes, declarations and properties as they will be capitalized making the change unnecessary in this case sensitive language
            ReplaceReservedNames(generatedCode, new CSharpReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}", new HashSet<Type>{ typeof(CodeClass), typeof(CodeClass.Declaration), typeof(CodeProperty) }); 
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
            AddSerializationModulesImport(generatedCode);
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
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass &&
                currentClass.IsOfKind(CodeClassKind.Model) &&
                currentClass.StartBlock is CodeClass.Declaration declaration) {
                declaration.AddImplements(new CodeType {
                    IsExternal = true,
                    Name = $"IParsable",
                });
                (currentClass.Parent is CodeClass parentClass &&
                parentClass.StartBlock is CodeClass.Declaration parentDeclaration ? 
                    parentDeclaration :
                    declaration)
                    .AddUsings(new CodeUsing {
                        Name = "Microsoft.Kiota.Abstractions.Serialization"
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static readonly Tuple<Func<CodeElement, bool>, string, string[]>[] defaultNamespaces = new Tuple<Func<CodeElement, bool>, string, string[]>[] { 
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.HttpCore),
                "Microsoft.Kiota.Abstractions", new string[] { "IHttpCore"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "Microsoft.Kiota.Abstractions", new string[] {"HttpMethod", "RequestInformation", "IMiddlewareOption"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft.Kiota.Abstractions", new string[] {"IResponseHandler"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
                "Microsoft.Kiota.Abstractions.Serialization", new string[] {"ISerializationWriter"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
                "Microsoft.Kiota.Abstractions.Serialization", new string[] {"IParseNode"}),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
                "Microsoft.Kiota.Abstractions.Serialization", new string[] {"IParsable"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
                "Microsoft.Kiota.Abstractions.Serialization", new string[] {"IParsable"}),
            new (x => x is CodeClass || x is CodeEnum,
                "System", new string[] {"String"}),
            new (x => x is CodeClass,
                "System.Collections.Generic", new string[] {"List", "Dictionary"}),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
                "System.IO", new string[] {"Stream"}),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.Threading.Tasks", new string[] {"Task"}),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model, CodeClassKind.RequestBuilder),
                "System.Linq", new string[] {"Enumerable"}),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                        method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
                "Microsoft.Kiota.Abstractions.Store", new string[] { "IBackingStoreFactory", "IBackingStoreFactorySingleton"}),
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
                "Microsoft.Kiota.Abstractions.Store", new string[] { "IBackingStore", "IBackedModel", "BackingStoreFactorySingleton" }),
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
    }
}
