using System;
using Kiota.Builder.Configuration;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Validations;
using Xunit;

namespace Kiota.Builder.Tests.Validation;
public class ValidationRuleSetExtensionsTests {
    [Fact]
    public void Defensive() {
        Assert.Throws<ArgumentNullException>(() => ValidationRuleSetExtensions.AddKiotaValidationRules(null, new()));
        ValidationRuleSetExtensions.AddKiotaValidationRules(new(), null);
    }
    [Fact]
    public void DisablesAllRules() {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = new() { "all" } };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.Empty(ruleSet);
    }
    [Fact]
    public void DisablesNoRule() {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = new() };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.NotEmpty(ruleSet);
    }
    [Fact]
    public void DisablesOneRule() {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = new() { nameof(NoServerEntry) } };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.NotEmpty(ruleSet);
        Assert.DoesNotContain(ruleSet, static x => x.GetType() == typeof(NoServerEntry));
    }
}
