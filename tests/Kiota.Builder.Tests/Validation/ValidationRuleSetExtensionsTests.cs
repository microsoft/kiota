using System;
using Kiota.Builder.Configuration;
using Kiota.Builder.Validation;
using Microsoft.OpenApi;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class ValidationRuleSetExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Throws<ArgumentNullException>(() => ValidationRuleSetExtensions.AddKiotaValidationRules(null, new()));
        ValidationRuleSetExtensions.AddKiotaValidationRules(new(), null);
    }
    [Fact]
    public void DisablesAllRules()
    {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = new() { "all" } };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.Empty(ruleSet.Rules);
    }
    [Fact]
    public void DisablesNoRule()
    {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = new() };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.NotEmpty(ruleSet.Rules);
    }
    [Fact]
    public void DisablesOneRule()
    {
        var ruleSet = new ValidationRuleSet();
        var configuration = new GenerationConfiguration { DisabledValidationRules = [nameof(NoServerEntry)] };
        ruleSet.AddKiotaValidationRules(configuration);
        Assert.NotEmpty(ruleSet.Rules);
        Assert.DoesNotContain(ruleSet.Rules, static x => x.GetType() == typeof(NoServerEntry));
    }
}
