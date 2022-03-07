using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class PhpRefiner: CommonLanguageRefiner
    {
        private static readonly CodeUsingDeclarationNameComparer usingComparer = new();
        public PhpRefiner(GenerationConfiguration configuration) : base(configuration) { }
        
        
        public override void Refine(CodeNamespace generatedCode)
        {
            ReplaceReservedNames(generatedCode, new PhpReservedNamesProvider(), reservedWord => $"Escaped{reservedWord.ToFirstCharacterUpperCase()}");
            AddConstructorsForDefaultValues(generatedCode, true);
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode, false, false);
            AddDiscriminatorMappingsUsingsToParentClasses(
                generatedCode,
                "ParseNode",
                addUsings: false
            );
            CorrectParameterType(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            MakeModelPropertiesNullable(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "ById");
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            ReplaceDefaultSerializationModules(generatedCode, "Microsoft\\Kiota\\Serialization\\Json\\JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "Microsoft\\Kiota\\Serialization\\Json\\JsonParseNodeFactory");
            AliasUsingWithSameSymbol(generatedCode);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore
                },
                _configuration.UsesBackingStore,
                true,
                "get",
                "set");
            AddParsableImplementsForModelClasses(generatedCode, "Parsable");
            ReplaceBinaryByNativeType(generatedCode, "StreamInterface", "Psr\\Http\\Message", true);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
        }
        private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "DateOnly",("Date", new CodeUsing
                {
                    Name = "Date",
                    Declaration = new CodeType
                    {
                        Name = "Microsoft\\Kiota\\Abstractions\\Types",
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
                        Name = "Microsoft\\Kiota\\Abstractions\\Types",
                        IsExternal = true,
                    },
                })
            },
            {
                "TimeSpan", ("DateInterval", new CodeUsing()
                {
                    Name = "DateInterval",
                    Declaration = new CodeType
                    {
                        Name = "",
                        IsExternal = true
                    }
                })
            }
        };
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
            new (x => x is CodeEnum, "Microsoft\\Kiota\\Abstractions\\", "Enum"),
            new(x => x is CodeProperty property && property.Type.Name.Equals("DateTime", StringComparison.OrdinalIgnoreCase), "", "DateTime"),
            new(x => x is CodeProperty property && property.Type.Name.Equals("DateTimeOffset", StringComparison.OrdinalIgnoreCase), "", "DateTime"),
            new(x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor), "Microsoft\\Kiota\\Abstractions", "ApiClientBuilder")
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
            } else if (currentProperty.Type.Name.Equals("DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                currentProperty.Type.Name = "DateTime";
            }
            CorrectDateTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        }

        private static void CorrectMethodType(CodeMethod method)
        {
            if (method.IsOfKind(CodeMethodKind.Deserializer))
            {
                method.ReturnType.Name = "array";
            } else if (method.IsOfKind(CodeMethodKind.RequestExecutor))
            {
                method.ReturnType = new CodeType() {Name = method.ReturnType.Name, IsExternal = true, IsNullable = false};
            }
        }
        private static void CorrectParameterType(CodeElement codeElement)
        {
            var currentMethod = codeElement as CodeMethod;
            var parameters = currentMethod?.Parameters;
            var codeParameters = parameters as CodeParameter[] ?? parameters?.ToArray();
            codeParameters?.Where(x => x.IsOfKind(CodeParameterKind.Options, CodeParameterKind.Headers)).ToList().ForEach(x =>
            {
                x.Type.Name = "array";
            });
            codeParameters?.Where(x => x.IsOfKind(CodeParameterKind.ParseNode)).ToList().ForEach(x =>
            {
                x.Type.Name = "ParseNode";
            });
            CrawlTree(codeElement, CorrectParameterType);
        }

        private static void AliasUsingWithSameSymbol(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.StartBlock != null && currentClass.StartBlock.Usings.Any(x => !x.IsExternal)) {
                var duplicatedSymbolsUsings = currentClass.StartBlock.Usings
                    .Distinct(usingComparer)
                    .GroupBy(x => x.Declaration.Name, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .SelectMany(x => x)
                    .Union(currentClass.StartBlock
                        .Usings
                        .Where(x => !x.IsExternal)
                        .Where(x => x.Declaration
                            .Name
                            .Equals(currentClass.Name, StringComparison.OrdinalIgnoreCase)));
                foreach (var usingElement in duplicatedSymbolsUsings)
                {
                    var declaration = usingElement.Declaration.TypeDefinition?.Name;
                    if (string.IsNullOrEmpty(declaration)) continue;
                    var replacement = string.Join(string.Empty, usingElement.Declaration.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name
                        .Split(new[]{'\\', '.'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.ToFirstCharacterUpperCase())
                        .ToArray());
                    usingElement.Alias = $"{replacement}{declaration.ToFirstCharacterUpperCase()}";
                }
            }
            CrawlTree(currentElement, AliasUsingWithSameSymbol);
        }
    }
}
