using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class MarkdownRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public MarkdownRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() => {
            cancellationToken.ThrowIfCancellationRequested();
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            MoveClassesWithNamespaceNamesUnderNamespace(generatedCode);
            ConvertUnionTypesToWrapper(generatedCode,
                _configuration.UsesBackingStore
            );
            cancellationToken.ThrowIfCancellationRequested();
            RemoveHandlerFromRequestBuilder(generatedCode);
            RemoveUnusedParametersFromRequestExecutor(generatedCode);
            RemoveUnusedPropertiesFromRequestBuilder(generatedCode);
        }, cancellationToken);
    }

    protected void RemoveUnusedParametersFromRequestExecutor(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var codeMethods = currentClass.Methods.Where(x => x.Kind == CodeMethodKind.RequestExecutor);
            foreach (var codeMethod in codeMethods)
            {
                codeMethod.RemoveParametersByKind(CodeParameterKind.Cancellation);
            }
        }

        CrawlTree(currentElement, RemoveUnusedParametersFromRequestExecutor);
    }
    protected void RemoveUnusedPropertiesFromRequestBuilder(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            var codeProperties = currentClass.Properties.Where(x => x.Kind == CodePropertyKind.RequestAdapter);
            foreach (var codeProperty in codeProperties)
            {
                currentClass.RemoveChildElementByName(codeProperty.Name);
            }
        }

        CrawlTree(currentElement, RemoveUnusedPropertiesFromRequestBuilder);
    }


    protected static void CorrectPropertyType(CodeProperty currentProperty)
    {
        if(currentProperty.IsOfKind(CodePropertyKind.Options))
            currentProperty.DefaultValue = "new List<IRequestOption>()";
        else if(currentProperty.IsOfKind(CodePropertyKind.Headers))
            currentProperty.DefaultValue = $"new {currentProperty.Type.Name.ToFirstCharacterUpperCase()}()";
        CorrectCoreTypes(currentProperty.Parent as CodeClass, DateTypesReplacements, currentProperty.Type);
    }
    protected static void CorrectMethodType(CodeMethod currentMethod)
    {
        CorrectCoreTypes(currentMethod.Parent as CodeClass, DateTypesReplacements, currentMethod.Parameters
                                                .Select(x => x.Type)
                                                .Union(new[] { currentMethod.ReturnType })
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
