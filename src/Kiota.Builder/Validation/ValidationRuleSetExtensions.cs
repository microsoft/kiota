using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public static class ValidationRuleSetExtensions {
    public static void AddKiotaValidationRules(this ValidationRuleSet ruleSet) {
        ruleSet.Add(new NoServerEntry());
        ruleSet.Add(new MultipleServerEntries());
    }
}
