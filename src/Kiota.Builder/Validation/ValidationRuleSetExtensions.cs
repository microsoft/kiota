using System;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public static class ValidationRuleSetExtensions {
    public const string AllValidationRule = "all";
    public static void AddKiotaValidationRules(this ValidationRuleSet ruleSet, GenerationConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(ruleSet);
        configuration ??= new();
        if (configuration.DisabledValidationRules.Contains(AllValidationRule)) return;
        
        ruleSet.AddRuleIfEnabled(configuration, new NoServerEntry());
        ruleSet.AddRuleIfEnabled(configuration, new MultipleServerEntries());
        ruleSet.AddRuleIfEnabled(configuration, new GetWithBody());
        ruleSet.AddRuleIfEnabled(configuration, new KnownAndNotSupportedFormats());
        ruleSet.AddRuleIfEnabled(configuration, new InconsistentTypeFormatPair());
        ruleSet.AddRuleIfEnabled(configuration, new UrlFormEncodedComplex());
        ruleSet.AddRuleIfEnabled(configuration, new DivergentResponseSchema(configuration));
        ruleSet.AddRuleIfEnabled(configuration, new MissingDiscriminator(configuration));
    }
    private static void AddRuleIfEnabled<T>(this ValidationRuleSet ruleSet, GenerationConfiguration configuration, T instance) where T : ValidationRule {
        if(!configuration.DisabledValidationRules.Contains(instance.GetType().Name))
            ruleSet.Add(instance);
    }
}
