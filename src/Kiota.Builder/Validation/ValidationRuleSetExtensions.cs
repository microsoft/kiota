using System;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public static class ValidationRuleSetExtensions {
    public static void AddKiotaValidationRules(this ValidationRuleSet ruleSet) {
        ArgumentNullException.ThrowIfNull(ruleSet);
        ruleSet.Add(new NoServerEntry());
        ruleSet.Add(new MultipleServerEntries());
        ruleSet.Add(new GetWithBody());
    }
}
