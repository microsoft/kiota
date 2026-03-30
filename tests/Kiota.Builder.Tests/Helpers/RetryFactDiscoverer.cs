using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Kiota.Builder.Tests.Helpers;

public class RetryFactDiscoverer : IXunitTestCaseDiscoverer
{
    public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);

        if (testMethod.Parameters.Count != 0)
        {
            return Error(details, "[Fact] methods are not allowed to have parameters. Did you mean to use [Theory]?");
        }

        if (testMethod.IsGenericMethodDefinition)
        {
            return Error(details, "[Fact] methods are not allowed to be generic.");
        }

        if (factAttribute is not RetryFactAttribute retryFactAttribute)
        {
            return Error(
                details,
                "{0} was decorated on [{1}] which is not compatible with {2}",
                typeof(RetryFactDiscoverer).SafeName(),
                factAttribute.GetType().SafeName(),
                typeof(RetryFactAttribute).SafeName());
        }

        var maxRetries = retryFactAttribute.MaxRetries < 1 ? 5 : retryFactAttribute.MaxRetries;
        var delayMs = retryFactAttribute.DelayMilliseconds < 1 ? 500 : retryFactAttribute.DelayMilliseconds;

        var retryTestCase = new RetryTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.Explicit,
            details.SkipExceptions,
            details.SkipReason,
            details.SkipType,
            details.SkipUnless,
            details.SkipWhen,
            GetTraits(testMethod),
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            timeout: details.Timeout,
            maxRetries: maxRetries,
            delayMilliseconds: delayMs);

        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(new[] { retryTestCase });
    }

    private static ValueTask<IReadOnlyCollection<IXunitTestCase>> Error(
        (string TestCaseDisplayName, bool Explicit, Type[] SkipExceptions, string SkipReason, Type SkipType, string SkipUnless, string SkipWhen, string SourceFilePath, int? SourceLineNumber, int Timeout, string UniqueID, IXunitTestMethod ResolvedTestMethod) details,
        string format,
        params object[] args)
    {
        var message = string.Format(CultureInfo.CurrentCulture, format, args);
        var error = new ExecutionErrorTestCase(
            details.ResolvedTestMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.SourceFilePath,
            details.SourceLineNumber,
            message);
        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(new[] { error });
    }

    private static Dictionary<string, HashSet<string>> GetTraits(IXunitTestMethod testMethod)
    {
        var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var trait in testMethod.Traits)
        {
            traits[trait.Key] = new HashSet<string>(trait.Value, StringComparer.OrdinalIgnoreCase);
        }

        return traits;
    }
}
