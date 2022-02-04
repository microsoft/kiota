using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class ShellRefiner : CSharpRefiner, ILanguageRefiner
    {
        public ShellRefiner(GenerationConfiguration configuration) : base(configuration) { }
        public override void Refine(CodeNamespace generatedCode)
        {
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddRawUrlConstructorOverload(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            CreateCommandBuilders(generatedCode);
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
                new HashSet<Type> { typeof(CodeClass), typeof(CodeClass.Declaration), typeof(CodeProperty), typeof(CodeUsing), typeof(CodeNamespace), typeof(CodeMethod) }
            );
            DisambiguatePropertiesWithClassNames(generatedCode);
            AddConstructorsForDefaultValues(generatedCode, false);
            AddSerializationModulesImport(generatedCode);
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
                    method.ReturnType.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex;
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
                    clone.SimpleName = cmdName;
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
                        OriginalMethod = clientConstructor,
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
            codeMethod.SimpleName = navProperty.Name;
            return codeMethod;
        }

        private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = new AdditionalUsingEvaluator[] {
            new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
                "Microsoft.Kiota.Abstractions", "IRequestAdapter"),
            new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
                "Microsoft.Kiota.Abstractions", "HttpMethod", "RequestInformation", "IRequestOption"),
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
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.CommandLine",  "Command", "RootCommand"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "Microsoft.Graph.Cli.Core.IO",  "IOutputFormatterFactory"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.Text",  "Encoding"),
        };


    }
}
