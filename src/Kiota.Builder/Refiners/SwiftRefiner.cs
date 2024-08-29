﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class SwiftRefiner : CommonLanguageRefiner
{
    public SwiftRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapitalizeNamespacesFirstLetters(generatedCode);
            AddRootClassForExtensions(generatedCode);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                false,
                static x => $"By{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterUpperCase(),
                GenerationLanguage.Swift);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNames(
                generatedCode,
                new SwiftReservedNamesProvider(),
                x => $"{x}_escaped");
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                static s => s
            );
            cancellationToken.ThrowIfCancellationRequested();
            AddPropertiesAndMethodTypesImports(
                generatedCode,
                true,
                false,
                true);
            AddDefaultImports(
                generatedCode,
                defaultUsingEvaluators);
            RemoveUntypedNodePropertyValues(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            CorrectCoreType(
                generatedCode,
                CorrectMethodType,
                CorrectPropertyType,
                CorrectImplements);
        }, cancellationToken);
    }
    private static readonly AdditionalUsingEvaluator[] defaultUsingEvaluators = {
        new (x => x is CodeProperty prop && prop.IsOfKind(CodePropertyKind.RequestAdapter),
            "MicrosoftKiotaAbstractions", "RequestAdapter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestGenerator),
            "MicrosoftKiotaAbstractions", "RequestInformation", "HttpMethod", "RequestOption"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.RequestExecutor),
            "MicrosoftKiotaAbstractions", "ResponseHandler"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Serializer),
            "MicrosoftKiotaAbstractions", "SerializationWriter"),
        new (x => x is CodeMethod method && method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Factory),
            "MicrosoftKiotaAbstractions", "ParseNode", "Parsable"),
        new (x => x is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model),
            "MicrosoftKiotaAbstractions", "Parsable"),
        new (x => x is CodeClass @class && @class.IsOfKind(CodeClassKind.Model) &&
                                            (@class.Properties.Any(x => x.IsOfKind(CodePropertyKind.AdditionalData)) ||
                                            @class.StartBlock.Implements.Any(x => KiotaBuilder.AdditionalHolderInterface.Equals(x.Name, StringComparison.OrdinalIgnoreCase))),
            "MicrosoftKiotaAbstractions", "AdditionalDataHolder"),
    };//TODO add backing store types once we have them defined
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
    }
    private static void CorrectMethodType(CodeMethod currentMethod)
    {
        var parentClass = currentMethod.Parent as CodeClass;
        if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator))
        {
            if (currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                currentMethod.Parameters.Where(x => x.Type.Name.Equals("IResponseHandler", StringComparison.Ordinal)).ToList().ForEach(x =>
                {
                    x.Type.Name = "ResponseHandler";
                    x.Type.IsNullable = false; //no pointers
                });
            else if (currentMethod.IsOfKind(CodeMethodKind.RequestGenerator))
                currentMethod.ReturnType.IsNullable = true;
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.Serializer))
            currentMethod.Parameters.Where(x => x.Type.Name.Equals("ISerializationWriter", StringComparison.Ordinal)).ToList().ForEach(x => x.Type.Name = "SerializationWriter");
        else if (currentMethod.IsOfKind(CodeMethodKind.Deserializer))
        {
            currentMethod.ReturnType.Name = "[String:FieldDeserializer<T>][String:FieldDeserializer<T>]";
            currentMethod.Name = "getFieldDeserializers";
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.ClientConstructor, CodeMethodKind.Constructor, CodeMethodKind.RawUrlConstructor))
        {
            var rawUrlParam = currentMethod.Parameters.OfKind(CodeParameterKind.RawUrl);
            if (rawUrlParam != null)
                rawUrlParam.Type.IsNullable = false;
            currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.RequestAdapter))
                .Where(x => x.Type.Name.StartsWith('I'))
                .ToList()
                .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
        else if (currentMethod.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility, CodeMethodKind.RequestBuilderWithParameters, CodeMethodKind.RequestBuilderBackwardCompatibility, CodeMethodKind.Factory))
        {
            currentMethod.ReturnType.IsNullable = true;
            if (currentMethod.Parameters.OfKind(CodeParameterKind.ParseNode) is CodeParameter parseNodeParam)
            {
                parseNodeParam.Type.Name = parseNodeParam.Type.Name[1..];
                parseNodeParam.Type.IsNullable = false;
            }
            if (currentMethod.IsOfKind(CodeMethodKind.Factory))
                currentMethod.ReturnType = new CodeType { Name = "Parsable", IsNullable = false, IsExternal = true };
        }
        CorrectCoreTypes(parentClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
                                                .ToArray());
    }
    private static readonly Dictionary<string, (string, CodeUsing?)> DateTypesReplacements = new(StringComparer.OrdinalIgnoreCase) {
        {"DateTimeOffset", ("Date", new CodeUsing {
                                        Name = "Date",//TODO
                                        Declaration = new CodeType {
                                            Name = "Foundation",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("Date", new CodeUsing {
                                        Name = "Date",//TODO
                                        Declaration = new CodeType {
                                            Name = "Foundation",
                                            IsExternal = true,
                                        },
                                    })},
        {"DateOnly", ("Date", new CodeUsing {
                                Name = "Date",
                                Declaration = new CodeType {
                                    Name = "Foundation",
                                    IsExternal = true,
                                },
                            })},
        {"TimeOnly", ("Date", new CodeUsing {
                                Name = "Date",//TODO
                                Declaration = new CodeType {
                                    Name = "Foundation",
                                    IsExternal = true,
                                },
                            })},
    };
    private static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if (currentProperty.Type != null)
        {
            if (currentProperty.IsOfKind(CodePropertyKind.RequestAdapter))
            {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "RequestAdapter";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if (currentProperty.IsOfKind(CodePropertyKind.AdditionalData))
            {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "[String:Any]";
                currentProperty.DefaultValue = $"{currentProperty.Type.Name}()";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.PathParameters))
            {
                currentProperty.Type.IsNullable = true;
                currentProperty.Type.Name = "[String:String]";
                if (!string.IsNullOrEmpty(currentProperty.DefaultValue))
                    currentProperty.DefaultValue = $"{currentProperty.Type.Name}()";
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.Options))
            {
                currentProperty.Type.IsNullable = false;
                currentProperty.Type.Name = "RequestOption";
                currentProperty.Type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
            }
            else if (currentProperty.IsOfKind(CodePropertyKind.QueryParameter) && currentProperty.Parent is CodeClass parentClass)
                currentProperty.Type.Name = $"{parentClass.Name}{currentProperty.Type.Name}";
            CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
        }
    }

    private static void CapitalizeNamespacesFirstLetters(CodeElement current)
    {
        if (current is CodeNamespace currentNamespace)
            currentNamespace.Name = currentNamespace.Name.Split('.').Select(static x => x.ToFirstCharacterUpperCase()).Aggregate(static (x, y) => $"{x}.{y}");
        CrawlTree(current, CapitalizeNamespacesFirstLetters);
    }
    private void AddRootClassForExtensions(CodeElement current)
    {
        if (current is CodeNamespace currentNamespace &&
            currentNamespace.FindNamespaceByName(_configuration.ClientNamespaceName) is CodeNamespace clientNamespace)
        {
            clientNamespace.AddClass(new CodeClass
            {
                Name = clientNamespace.Name.Split('.', StringSplitOptions.RemoveEmptyEntries).Last().ToFirstCharacterUpperCase(),
                Kind = CodeClassKind.BarrelInitializer,
                Documentation = new()
                {
                    DescriptionTemplate = "Root class for extensions",
                },
            });
        }
    }
}
