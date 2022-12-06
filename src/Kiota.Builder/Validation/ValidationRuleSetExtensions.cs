using System;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public static class ValidationRuleSetExtensions {
    public static void AddKiotaValidationRules(this ValidationRuleSet ruleSet, GenerationConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(ruleSet);
        configuration ??= new();
        ruleSet.Add(new NoServerEntry());
        ruleSet.Add(new MultipleServerEntries());
        ruleSet.Add(new GetWithBody());
        ruleSet.Add(new KnownAndNotSupportedFormats());
        ruleSet.Add(new InconsistentTypeFormatPair());
        ruleSet.Add(new InconsistentTypeFormatPair());
        ruleSet.Add(new UrlFormEncodedComplex());
        ruleSet.Add(new DivergentResponseSchema(configuration));
    }
}
