using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Kiota.Builder.Tests.Helpers;

public class RetryFactDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        var maxRetries = factAttribute.GetNamedArgument<int>(nameof(RetryFactAttribute.MaxRetries));
        var delayMs = factAttribute.GetNamedArgument<int>(nameof(RetryFactAttribute.DelayMilliseconds));

        if (maxRetries < 1)
            maxRetries = 5;
        if (delayMs < 1)
            delayMs = 500;

        yield return new RetryTestCase(
            diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod,
            maxRetries,
            delayMs);
    }
}
