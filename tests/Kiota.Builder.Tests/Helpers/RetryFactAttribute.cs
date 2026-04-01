using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Kiota.Builder.Tests.Helpers;

/// <summary>
/// A custom xUnit [Fact] that retries on failure with exponential backoff.
/// Use for tests that are flaky due to external service throttling.
/// </summary>
[XunitTestCaseDiscoverer(typeof(RetryFactDiscoverer))]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RetryFactAttribute : FactAttribute
{
    public RetryFactAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }

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
