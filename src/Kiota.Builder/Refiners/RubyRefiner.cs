using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class RubyRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public RubyRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() => {
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode, false, "_by_id");
            AddPropertiesAndMethodTypesImports(generatedCode, false, false, true);
            RemoveCancellationParameter(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddParsableImplementsForModelClasses(generatedCode, "MicrosoftKiotaAbstractions::Parsable");
            AddInheritedAndMethodTypesImports(generatedCode);
            AddDefaultImports(generatedCode, defaultUsingEvaluators);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType, CorrectImplements);
            cancellationToken.ThrowIfCancellationRequested();
            AddGetterAndSetterMethods(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                    CodePropertyKind.AdditionalData,
                    CodePropertyKind.BackingStore,
                },
                _configuration.UsesBackingStore,
                true,
                string.Empty,
                string.Empty,
                string.Empty);
            AddConstructorsForDefaultValues(
                generatedCode,
                true,
                false,
                new[] { CodeClassKind.RequestConfiguration });
            ReplaceReservedNames(generatedCode, new RubyReservedNamesProvider(), x => $"{x}_escaped");
            AddNamespaceModuleImports(generatedCode , _configuration.ClientNamespaceName);
            var defaultConfiguration = new GenerationConfiguration();
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceDefaultSerializationModules(
                generatedCode,
                defaultConfiguration.Serializers,
                new (StringComparer.OrdinalIgnoreCase) {
                    "microsoft_kiota_serialization.JsonSerializationWriterFactory"});
            ReplaceDefaultDeserializationModules(
                generatedCode,
                defaultConfiguration.Deserializers,
                new (StringComparer.OrdinalIgnoreCase) {
                    "microsoft_kiota_serialization.JsonParseNodeFactory"});
            AddSerializationModulesImport(generatedCode,
                                        new [] { "microsoft_kiota_abstractions.ApiClientBuilder",
                                                "microsoft_kiota_abstractions.SerializationWriterFactoryRegistry" },
                                        new [] { "microsoft_kiota_abstractions.ParseNodeFactoryRegistry" });
            AddQueryParameterMapperMethod(
                generatedCode
            );
        }, cancellationToken);
    }
    private static void CorrectMethodType(CodeMethod currentMethod) {
        if(currentMethod.IsOfKind(CodeMethodKind.Factory) && currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                    .Select(x => x.Type)
                                    .Union(new[] { currentMethod.ReturnType})
                                    .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing)> DateTypesReplacements = new (StringComparer.OrdinalIgnoreCase) {
        {"DateTimeOffset", ("DateTime", new CodeUsing {
                                        Name = "DateTime",
                                        Declaration = new CodeType {
                                            Name = "date",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("MicrosoftKiotaAbstractions::ISODuration", new CodeUsing {
                                        Name = "MicrosoftKiotaAbstractions::ISODuration",
                                        Declaration = new CodeType {
                                            Name = "github.com/microsoft/kiota/abstractions/ruby/microsoft_kiota_abstractions/lib/microsoft_kiota_abstractions/serialization",
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", ("Date", new CodeUsing {
                                Name = "Date",
                                Declaration = new CodeType {
                                    Name = "date",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", ("Time", new CodeUsing {
                                Name = "Time",
                                Declaration = new CodeType {
                                    Name = "time",
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty) {
        if(currentProperty.IsOfKind(CodePropertyKind.PathParameters, CodePropertyKind.AdditionalData)) {
            currentProperty.Type.IsNullable = true;
            if(!string.IsNullOrEmpty(currentProperty.DefaultValue))
                currentProperty.DefaultValue = "Hash.new";
        }

        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        
    }
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = { 
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "microsoft_kiota_abstractions", "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "microsoft_kiota_abstractions", "HttpMethod", "RequestInformation", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "microsoft_kiota_abstractions", "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "microsoft_kiota_abstractions", "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer),
            "microsoft_kiota_abstractions", "ParseNode"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model),
            "microsoft_kiota_abstractions", "Parsable"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "microsoft_kiota_abstractions", "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) && @class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)),
            "microsoft_kiota_abstractions", "AdditionalDataHolder"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.ClientConstructor) &&
                    method.Parameters.Any(y => y.IsOfKind(CodeParameterKind.BackingStore)),
            "microsoft_kiota_abstractions", "BackingStoreFactory", "BackingStoreFactorySingleton"),
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.BackingStore),
            "microsoft_kiota_abstractions", "BackingStore", "BackedModel", "BackingStoreFactorySingleton" ),
    };
    protected static void AddInheritedAndMethodTypesImports(CodeElement currentElement) {
        if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) 
            && currentClass.StartBlock.Inherits != null) {
            currentClass.AddUsing(new CodeUsing { Name = currentClass.StartBlock.Inherits.Name, Declaration = currentClass.StartBlock.Inherits});
        }
        CrawlTree(currentElement, x => AddInheritedAndMethodTypesImports(x));
    }

    protected void AddNamespaceModuleImports(CodeElement current, string clientNamespaceName) {
        const string dot = ".";
        if(current is CodeClass currentClass) {
            var Module = currentClass.GetImmediateParentOfType<CodeNamespace>();
            if(!string.IsNullOrEmpty(Module.Name)){
                var modulesProperties = Module.Name.Replace(clientNamespaceName+dot, string.Empty).Split(dot);
                for (int i = modulesProperties.Length - 1; i >= 0; i--){
                    var prefix = string.Concat(Enumerable.Repeat("../", modulesProperties.Length -i-1));
                    var usingName = modulesProperties[i].ToSnakeCase();
                    currentClass.AddUsing(new CodeUsing { 
                        Name = usingName,
                        Declaration = new CodeType {
                            IsExternal = false,
                            Name = $"{(string.IsNullOrEmpty(prefix) ? "./" : prefix)}{usingName}",
                        }
                    });
                }
            }
        }
        CrawlTree(current, c => AddNamespaceModuleImports(c, clientNamespaceName));
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block) {
        block.Implements
            .Where(static x => "IAdditionalDataHolder".Equals(x.Name, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(static x => x.Name = "MicrosoftKiotaAbstractions::AdditionalDataHolder"); 
    }
}
