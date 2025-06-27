using System;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi;

namespace Kiota.Builder.Validation;

public static class ValidationRuleSetExtensions
{
    public const string AllValidationRule = "all";
    public static void AddKiotaValidationRules(this ValidationRuleSet ruleSet, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        configuration ??= new();
        if (configuration.DisabledValidationRules.Contains(AllValidationRule)) return;

        ruleSet.AddRuleIfEnabled(configuration, new NoServerEntry(), typeof(OpenApiDocument));
        ruleSet.AddRuleIfEnabled(configuration, new MultipleServerEntries(), typeof(OpenApiDocument));
        ruleSet.AddRuleIfEnabled(configuration, new GetWithBody(), typeof(IOpenApiPathItem));
        ruleSet.AddRuleIfEnabled(configuration, new KnownAndNotSupportedFormats(), typeof(IOpenApiSchema));
        ruleSet.AddRuleIfEnabled(configuration, new InconsistentTypeFormatPair(), typeof(IOpenApiSchema));
        ruleSet.AddRuleIfEnabled(configuration, new UrlFormEncodedComplex(), typeof(OpenApiOperation));
        ruleSet.AddRuleIfEnabled(configuration, new DivergentResponseSchema(configuration), typeof(OpenApiOperation));
        ruleSet.AddRuleIfEnabled(configuration, new MissingDiscriminator(configuration), typeof(OpenApiDocument));
    }
    private static void AddRuleIfEnabled<T>(this ValidationRuleSet ruleSet, GenerationConfiguration configuration, T instance, Type ruleType) where T : ValidationRule
    {
        if (!configuration.DisabledValidationRules.Contains(typeof(T).Name))
            ruleSet.Add(ruleType, instance);
    }
}
