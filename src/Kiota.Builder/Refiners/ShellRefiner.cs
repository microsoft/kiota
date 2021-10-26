using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class ShellRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public ShellRefiner(GenerationConfiguration configuration) : base(configuration) { }
        public override void Refine(CodeNamespace generatedCode)
        {
            // Remove PathSegment field
            // Convert Properties to AddCommand
            AddDefaultImports(generatedCode);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            //RemoveModelClasses(generatedCode);
            //RemoveEnums(generatedCode);
            //RemoveConstructors(generatedCode);
            CreateCommandBuilders(generatedCode);
            AddAsyncSuffix(generatedCode);
            AddInnerClasses(generatedCode, false);
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            ReplaceReservedNames(generatedCode, new CSharpReservedNamesProvider(), x => $"@{x.ToFirstCharacterUpperCase()}");
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
            AddSerializationModulesImport(generatedCode);
        }
        private static void DisambiguatePropertiesWithClassNames(CodeElement currentElement)
        {
            if (currentElement is CodeClass currentClass)
            {
                var sameNameProperty = currentClass
                                                .GetChildElements(true)
                                                .OfType<CodeProperty>()
                                                .FirstOrDefault(x => x.Name.Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase));
                if (sameNameProperty != null)
                {
                    currentClass.RemoveChildElement(sameNameProperty);
                    sameNameProperty.SerializationName ??= sameNameProperty.Name;
                    sameNameProperty.Name = $"{sameNameProperty.Name}_prop";
                    currentClass.AddProperty(sameNameProperty);
                }
            }
            CrawlTree(currentElement, DisambiguatePropertiesWithClassNames);
        }
        private static void MakeEnumPropertiesNullable(CodeElement currentElement)
        {
            if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
                currentClass.GetChildElements(true)
                            .OfType<CodeProperty>()
                            .Where(x => x.Type is CodeType propType && propType.TypeDefinition is CodeEnum)
                            .ToList()
                            .ForEach(x => x.Type.IsNullable = true);
            CrawlTree(currentElement, MakeEnumPropertiesNullable);
        }
        private static void RemoveModelClasses(CodeElement currentElement)
        {
            if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model))
            {
                var codeNamespace = currentClass.Parent as CodeNamespace;
                codeNamespace.RemoveChildElement(currentClass);
            }
            CrawlTree(currentElement, RemoveModelClasses);
        }

        private static void RemoveEnums(CodeElement currentElement)
        {
            if (currentElement is CodeEnum currentEnum)
            {
                var codeNamespace = currentElement.Parent as CodeNamespace;
                codeNamespace.RemoveChildElement(currentEnum);
            }
            CrawlTree(currentElement, RemoveEnums);
        }

        private static void RemoveConstructors(CodeElement currentElement)
        {
            if (currentElement is CodeMethod currentMethod && currentMethod.IsOfKind(CodeMethodKind.Constructor))
            {
                var codeClass = currentElement.Parent as CodeClass;
                codeClass.RemoveChildElement(currentMethod);
            }
            CrawlTree(currentElement, RemoveConstructors);
        }

        private static void CreateCommandBuilders(CodeElement currentElement)
        {
            if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
            {
                // Replace Nav Properties with BuildXXXCommand methods
                var navProperties = currentClass.GetChildElements().OfType<CodeProperty>().Where(e => e.IsOfKind(CodePropertyKind.RequestBuilder));
                foreach (var navProp in navProperties)
                {
                    var method = CreateBuildCommandMethod(navProp, currentClass);
                    currentClass.AddMethod(method);
                    currentClass.RemoveChildElement(navProp);
                }

                // Build command for indexers
                var indexers = currentClass.GetChildElements().OfType<CodeIndexer>();
                var classHasIndexers = indexers.Any();
                foreach (var indexer in indexers)
                {
                    var method = new CodeMethod
                    {
                        Name = "BuildCommand",
                        IsAsync = false,
                        MethodKind = CodeMethodKind.CommandBuilder,
                        OriginalIndexer = indexer
                    };

                    method.ReturnType = CreateCommandType(method);
                    method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
                    currentClass.AddMethod(method);
                    currentClass.RemoveChildElement(indexer);
                }

                // Clone executors & convert to build command
                var requestMethods = currentClass.GetChildElements().OfType<CodeMethod>().Where(e => e.IsOfKind(CodeMethodKind.RequestExecutor));
                foreach (var requestMethod in requestMethods)
                {
                    CodeMethod clone = requestMethod.Clone() as CodeMethod;
                    var cmdName = clone.Name;
                    if (classHasIndexers)
                    {
                        if (clone.HttpMethod == HttpMethod.Get) cmdName = "List";
                        if (clone.HttpMethod == HttpMethod.Post) cmdName = "Create";
                    }

                    clone.IsAsync = false;
                    clone.Name = $"Build{cmdName}Command";
                    clone.ReturnType = CreateCommandType(clone);
                    clone.MethodKind = CodeMethodKind.CommandBuilder;
                    clone.OriginalMethod = requestMethod;
                    clone.ClearParameters();
                    currentClass.AddMethod(clone);
                }

                // Build root command
                var clientConstructor = currentClass.GetChildElements().OfType<CodeMethod>().FirstOrDefault(m => m.MethodKind == CodeMethodKind.ClientConstructor);
                if (clientConstructor != null)
                {
                    var rootMethod = new CodeMethod
                    {
                        Name = "BuildCommand",
                        IsAsync = false,
                        MethodKind = CodeMethodKind.CommandBuilder,
                        ReturnType = new CodeType { Name = "Command", IsExternal = true },
                        OriginalMethod = clientConstructor
                    };
                    currentClass.AddMethod(rootMethod);
                }
            }
            CrawlTree(currentElement, CreateCommandBuilders);
        }

        private static CodeType CreateCommandType(CodeElement parent)
        {
            return new CodeType
            {
                Name = "Command",
                IsExternal = true
            };
        }

        private static CodeMethod CreateBuildCommandMethod(CodeProperty navProperty, CodeClass parent)
        {
            var codeMethod = new CodeMethod();
            codeMethod.IsAsync = false;
            codeMethod.Name = $"Build{navProperty.Name.ToFirstCharacterUpperCase()}Command";
            codeMethod.MethodKind = CodeMethodKind.CommandBuilder;
            codeMethod.ReturnType = CreateCommandType(codeMethod);
            codeMethod.AccessedProperty = navProperty;
            return codeMethod;
        }

        private static readonly string[] defaultNamespacesForClasses = new string[] { "System", "System.Collections.Generic", "System.Linq" };
        private static readonly string[] defaultNamespacesForRequestBuilders = new string[] { "System.Threading.Tasks", "System.IO", "Microsoft.Kiota.Abstractions", "Microsoft.Kiota.Abstractions.Serialization", "System.CommandLine", "System.CommandLine.Invocation" };

        private static void AddDefaultImports(CodeElement current)
        {
            if (current is CodeClass currentClass)
            {
                currentClass.AddUsing(defaultNamespacesForClasses.Select(x => new CodeUsing { Name = x }).ToArray());
                if (currentClass.IsOfKind(CodeClassKind.RequestBuilder))
                    currentClass.AddUsing(defaultNamespacesForRequestBuilders.Select(x => new CodeUsing { Name = x }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private static void CapitalizeNamespacesFirstLetters(CodeElement current)
        {
            if (current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
        private static void AddAsyncSuffix(CodeElement currentElement)
        {
            if (currentElement is CodeMethod currentMethod && currentMethod.IsAsync)
                currentMethod.Name += "Async";
            CrawlTree(currentElement, AddAsyncSuffix);
        }
    }
}
