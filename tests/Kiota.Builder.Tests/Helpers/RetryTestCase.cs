using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Kiota.Builder.Tests.Helpers;

[Serializable]
public class RetryTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    private int maxRetries;
    private int delayMilliseconds;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryTestCase()
    {
    }

    public RetryTestCase(
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueID,
        bool @explicit,
        Type[] skipExceptions = null,
        string skipReason = null,
        Type skipType = null,
        string skipUnless = null,
        string skipWhen = null,
        Dictionary<string, HashSet<string>> traits = null,
        object[] testMethodArguments = null,
        string sourceFilePath = null,
        int? sourceLineNumber = null,
        int? timeout = null,
        int maxRetries = 5,
        int delayMilliseconds = 500)
        : base(
            testMethod,
            testCaseDisplayName,
            uniqueID,
            @explicit,
            skipExceptions,
            skipReason,
            skipType,
            skipUnless,
            skipWhen,
            traits,
            testMethodArguments,
            sourceFilePath,
            sourceLineNumber,
            timeout)
    {
        this.maxRetries = maxRetries;
        this.delayMilliseconds = delayMilliseconds;
    }

    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var delay = delayMilliseconds;
        RunSummary lastSummary = default;
        var hasLastSummary = false;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var replayBus = new ReplayMessageBus(messageBus);
            var attemptAggregator = aggregator.Clone();

            var summary = await XunitRunnerHelper.RunXunitTestCase(
                this,
                replayBus,
                cancellationTokenSource,
                attemptAggregator,
                explicitOption,
                constructorArguments).ConfigureAwait(false);

            var hasFailures = summary.Failed > 0 || attemptAggregator.HasExceptions;
            if (!hasFailures || attempt == maxRetries)
            {
                replayBus.ForwardAll();
                aggregator.Aggregate(attemptAggregator);
                return summary;
            }

            lastSummary = summary;
            hasLastSummary = true;

            messageBus.QueueMessage(
                new DiagnosticMessage(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[RetryFact] {0} failed on attempt {1}/{2}. Retrying in {3}ms...",
                        TestCaseDisplayName,
                        attempt,
                        maxRetries,
                        delay)));

            await Task.Delay(delay, cancellationTokenSource.Token).ConfigureAwait(false);
            delay *= 2;
        }

        return hasLastSummary ? lastSummary : new RunSummary { Total = 1, Failed = 1 };
    }

    protected override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);
        info.AddValue(nameof(maxRetries), maxRetries);
        info.AddValue(nameof(delayMilliseconds), delayMilliseconds);
    }

    protected override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);
        maxRetries = info.GetValue<int>(nameof(maxRetries));
        delayMilliseconds = info.GetValue<int>(nameof(delayMilliseconds));
    }

    private sealed class ReplayMessageBus(IMessageBus innerBus) : IMessageBus
    {
        private readonly ConcurrentQueue<IMessageSinkMessage> messages = new();

        public bool QueueMessage(IMessageSinkMessage message)
        {
            messages.Enqueue(message);
            return true;
        }

        public void ForwardAll()
        {
            while (messages.TryDequeue(out var message))
            {
                innerBus.QueueMessage(message);
            }
        }

        public void Dispose()
        {
        }
    }
}
