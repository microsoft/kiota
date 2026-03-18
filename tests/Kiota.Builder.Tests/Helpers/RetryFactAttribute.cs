using System;
using Xunit;
using Xunit.Sdk;

namespace Kiota.Builder.Tests.Helpers;

/// <summary>
/// A custom xUnit [Fact] that retries on failure with exponential backoff.
/// Use for tests that are flaky due to external service throttling.
/// </summary>
[XunitTestCaseDiscoverer("Kiota.Builder.Tests.Helpers.RetryFactDiscoverer", "Kiota.Builder.Tests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RetryFactAttribute : FactAttribute
{
    /// <summary>
    /// Maximum number of attempts before giving up (default: 5).
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Initial delay in milliseconds before the first retry (default: 500ms).
    /// The delay doubles on each subsequent retry (exponential backoff).
    /// </summary>
    public int DelayMilliseconds { get; set; } = 500;
}
