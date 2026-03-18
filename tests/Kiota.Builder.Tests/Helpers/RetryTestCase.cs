using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Kiota.Builder.Tests.Helpers;

[Serializable]
public class RetryTestCase : XunitTestCase
{
    private int _maxRetries;
    private int _delayMs;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryTestCase()
    {
    }

    public RetryTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        int maxRetries,
        int delayMs)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
        _maxRetries = maxRetries;
        _delayMs = delayMs;
    }

    public override async Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var delayMs = _delayMs;

        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            var interceptingBus = new InterceptingMessageBus(messageBus);

            var summary = await base.RunAsync(
                diagnosticMessageSink,
                interceptingBus,
                constructorArguments,
                new ExceptionAggregator(aggregator),
                cancellationTokenSource);

            if (summary.Failed == 0 || attempt == _maxRetries)
            {
                // Forward all intercepted messages on final attempt or on success
                interceptingBus.ForwardAll();
                return summary;
            }

            diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                $"[RetryFact] {DisplayName} failed on attempt {attempt}/{_maxRetries}. Retrying in {delayMs}ms..."));

            await Task.Delay(delayMs, cancellationTokenSource.Token);
            delayMs *= 2; // exponential backoff
        }

        // Should never reach here, but just in case
        return new RunSummary();
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        base.Serialize(data);
        data.AddValue(nameof(_maxRetries), _maxRetries);
        data.AddValue(nameof(_delayMs), _delayMs);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        base.Deserialize(data);
        _maxRetries = data.GetValue<int>(nameof(_maxRetries));
        _delayMs = data.GetValue<int>(nameof(_delayMs));
    }

    /// <summary>
    /// Message bus that captures messages so they can be replayed only when needed.
    /// This prevents intermediate failures from being reported to the test runner.
    /// </summary>
    private sealed class InterceptingMessageBus(IMessageBus innerBus) : IMessageBus
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<IMessageSinkMessage> _messages = new();

        public bool QueueMessage(IMessageSinkMessage message)
        {
            _messages.Enqueue(message);
            return true;
        }

        public void ForwardAll()
        {
            while (_messages.TryDequeue(out var message))
            {
                innerBus.QueueMessage(message);
            }
        }

        public void Dispose()
        {
            // Do not dispose the inner bus; it's owned by the framework.
        }
    }
}
