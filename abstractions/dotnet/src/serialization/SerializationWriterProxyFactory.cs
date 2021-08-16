using System;

namespace Microsoft.Kiota.Abstractions.Serialization {
    /// <summary>
    /// Proxy factory that allows the composition of before and after callbacks on existing factories.
    /// </summary>
    public class SerializationWriterProxyFactory : ISerializationWriterFactory {
        public string ValidContentType { get { return _concrete.ValidContentType; }}
        private readonly ISerializationWriterFactory _concrete;
        private readonly Action<IParsable> _onBefore;
        private readonly Action<IParsable> _onAfter;
        private readonly Action<IParsable, ISerializationWriter> _onStartSerialization;
        /// <summary>
        /// Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
        /// </summary>
        /// <param name="concrete">The concrete factory to wrap.</param>
        /// <param name="onBeforeSerialization">The callback to invoke before the serialization of any model object.</param>
        /// <param name="onAfterSerialization">The callback to invoke after the serialization of any model object.</param>
        /// <param name="onStartSerialization">The callback to invoke when serialization of the entire model has started.</param>
        public SerializationWriterProxyFactory(ISerializationWriterFactory concrete,
            Action<IParsable> onBeforeSerialization,
            Action<IParsable> onAfterSerialization,
            Action<IParsable, ISerializationWriter> onStartSerialization) {
            _concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            _onBefore = onBeforeSerialization;
            _onAfter = onAfterSerialization;
            _onStartSerialization = onStartSerialization;
        }
        public ISerializationWriter GetSerializationWriter(string contentType) {
            var writer = _concrete.GetSerializationWriter(contentType);
            var originalBefore = writer.OnBeforeObjectSerialization;
            var originalAfter = writer.OnAfterObjectSerialization;
            var originalStart = writer.OnStartObjectSerialization;
            writer.OnBeforeObjectSerialization = (x) => {
                _onBefore?.Invoke(x); // the callback set by the implementation (e.g. backing store)
                originalBefore?.Invoke(x); // some callback that might already be set on the target
            };
            writer.OnAfterObjectSerialization = (x) => {
                _onAfter?.Invoke(x);
                originalAfter?.Invoke(x);
            };
            writer.OnStartObjectSerialization = (x, y) => {
                _onStartSerialization?.Invoke(x, y);
                originalStart?.Invoke(x, y);
            };
            return writer;
        }
    }
}
