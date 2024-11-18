using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class HttpRefiner : CommonLanguageRefiner
{
    public HttpRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                false,
                static x => $"By{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterUpperCase(),
                GenerationLanguage.HTTP);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNames(
                generatedCode,
                new HttpReservedNamesProvider(),
                x => $"{x}_escaped");
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                static s => s
            );
            cancellationToken.ThrowIfCancellationRequested();
            SetBaseUrlForRequestBuilderMethods(generatedCode, GetBaseUrl(generatedCode));
            // Remove unused code from the DOM e.g Models, BarrelInitializers, e.t.c
            RemoveUnusedCodeElements(generatedCode);
            CorrectCoreType(
                generatedCode,
                CorrectMethodType,
                CorrectPropertyType,
                CorrectImplements);
        }, cancellationToken);
    }
    private static void CorrectImplements(ProprietableBlockDeclaration block)
    {
        block.ReplaceImplementByName(KiotaBuilder.AdditionalHolderInterface, "AdditionalDataHolder");
    }

    private string? GetBaseUrl(CodeElement element)
    {
        return element.GetImmediateParentOfType<CodeNamespace>()
                      .GetRootNamespace()?
                      .FindChildrenByName<CodeClass>(_configuration.ClientClassName)?
                      .FirstOrDefault()?
                      .Methods?
                      .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.ClientConstructor))?
                      .BaseUrl;
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
                                        Name = "Date",
                                        Declaration = new CodeType {
                                            Name = "Foundation",
                                            IsExternal = true,
                                        },
                                    })},
        {"TimeSpan", ("Date", new CodeUsing {
                                        Name = "Date",
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
                                Name = "Date",
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

    private static void SetBaseUrlForRequestBuilderMethods(CodeElement current, string? baseUrl)
    {
        if (baseUrl is not null && current is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Add a new property named BaseUrl and set its value to the baseUrl string
            var baseUrlProperty = new CodeProperty
            {
                Name = "BaseUrl",
                Kind = CodePropertyKind.Custom,
                Access = AccessModifier.Private,
                DefaultValue = baseUrl,
                Type = new CodeType { Name = "string", IsExternal = true }
            };
            codeClass.AddProperty(baseUrlProperty);
        }
        CrawlTree(current, (element) => SetBaseUrlForRequestBuilderMethods(element, baseUrl));
    }

    private static void RemoveUnusedCodeElements(CodeElement element)
    {
        if (!IsRequestBuilderClass(element) || IsBaseRequestBuilder(element) || IsRequestBuilderClassWithoutAnyHttpOperations(element))
        {
            var parentNameSpace = element.GetImmediateParentOfType<CodeNamespace>();
            parentNameSpace?.RemoveChildElement(element);
        }
        else
        {
            // Add path variables
            AddPathParameters(element);
        }
        CrawlTree(element, RemoveUnusedCodeElements);
    }

    private static void AddPathParameters(CodeElement element)
    {
        // Target RequestBuilder Classes only
        if (element is not CodeClass codeClass) return;

        var parent = element.GetImmediateParentOfType<CodeNamespace>().Parent;
        while (parent is not null)
        {
            var codeIndexer = parent.GetChildElements(false)
                .OfType<CodeClass>()
                .FirstOrDefault()?
                .GetChildElements(false)
                .OfType<CodeMethod>()
                .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));

            if (codeIndexer is not null)
            {
                // Retrieve all the parameters of kind CodeParameterKind.Custom
                var customParameters = codeIndexer.Parameters
                    .Where(param => param.IsOfKind(CodeParameterKind.Custom))
                    .ToList();

                // For each parameter:
                foreach (var param in customParameters)
                {
                    // Create a new property of kind CodePropertyKind.PathParameters using the parameter and add it to the codeClass
                    var pathParameterProperty = new CodeProperty
                    {
                        Name = param.Name,
                        Kind = CodePropertyKind.PathParameters,
                        Type = param.Type,
                        Access = AccessModifier.Public,
                        DefaultValue = param.DefaultValue,
                        SerializationName = param.SerializationName,
                        Documentation = param.Documentation
                    };
                    codeClass.AddProperty(pathParameterProperty);
                }
            }

            parent = parent.Parent?.GetImmediateParentOfType<CodeNamespace>();
        }
    }

    private static bool IsRequestBuilderClass(CodeElement element)
    {
        return element is CodeClass code && code.IsOfKind(CodeClassKind.RequestBuilder);
    }

    private static bool IsBaseRequestBuilder(CodeElement element)
    {
        return element is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            codeClass.Properties.Any(property => property.IsOfKind(CodePropertyKind.UrlTemplate) && string.Equals(property.DefaultValue, "\"{+baseurl}\"", StringComparison.Ordinal));
    }

    private static bool IsRequestBuilderClassWithoutAnyHttpOperations(CodeElement element)
    {
        return element is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder) &&
               !codeClass.Methods.Any(method => method.IsOfKind(CodeMethodKind.RequestExecutor));
    }
}
