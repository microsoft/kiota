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
            AddDefaultImports(generatedCode, additionalUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, _configuration.UsesBackingStore);
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, false);
            AddInnerClasses(generatedCode, false);
            AddParsableInheritanceForModelClasses(generatedCode, "IParsable");
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "Stream", "System.IO");
            MakeEnumPropertiesNullable(generatedCode);
            CreateCommandBuilders(generatedCode);
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

                    // ReturnType setter assigns the parent
                    method.ReturnType = CreateCommandType();
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
                        switch (clone.HttpMethod) {
                            case HttpMethod.Get:
                                cmdName = "List";
                                break;
                            case HttpMethod.Post:
                                cmdName = "Create";
                                break;
                        }
                    }

                    clone.IsAsync = false;
                    clone.Name = $"Build{cmdName}Command";
                    clone.Description = requestMethod.Description;
                    clone.ReturnType = CreateCommandType();
                    clone.MethodKind = CodeMethodKind.CommandBuilder;
                    clone.OriginalMethod = requestMethod;
                    clone.SimpleName = cmdName;
                    clone.ClearParameters();
                    currentClass.AddMethod(clone);
                    currentClass.RemoveChildElement(requestMethod);
                }

                // Build root command
                var clientConstructor = currentClass.GetChildElements().OfType<CodeMethod>().FirstOrDefault(m => m.MethodKind == CodeMethodKind.ClientConstructor);
                if (clientConstructor != null)
                {
                    var rootMethod = new CodeMethod
                    {
                        Name = "BuildCommand",
                        Description = clientConstructor.Description,
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

        private static CodeType CreateCommandType()
        {
            return new CodeType
            {
                Name = "Command",
                IsExternal = true,
            };
        }

        private static CodeMethod CreateBuildCommandMethod(CodeProperty navProperty, CodeClass parent)
        {
            var codeMethod = new CodeMethod
            {
                IsAsync = false,
                Name = $"Build{navProperty.Name.ToFirstCharacterUpperCase()}Command",
                MethodKind = CodeMethodKind.CommandBuilder
            };
            codeMethod.ReturnType = CreateCommandType();
            codeMethod.AccessedProperty = navProperty;
            codeMethod.SimpleName = navProperty.Name;
            codeMethod.Parent = parent;
            return codeMethod;
        }

        private static readonly AdditionalUsingEvaluator[] additionalUsingEvaluators = new AdditionalUsingEvaluator[] {
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.CommandLine",  "Command", "RootCommand", "IConsole"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "Microsoft.Kiota.Cli.Commons.IO", "IOutputFormatter", "IOutputFormatterFactory", "FormatterType"),
            new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.RequestBuilder),
                "System.Text",  "Encoding"),
        };


    }
}
