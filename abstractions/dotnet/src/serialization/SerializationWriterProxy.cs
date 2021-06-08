using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public abstract class SerializationWriterProxy : ISerializationWriter {
        private readonly ISerializationWriter _concrete;
        public SerializationWriterProxy(ISerializationWriter concrete, Action<IParsable> onBeforeSerialization, Action<IParsable> onAfterSerialization) {
            _concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            var originalOnBefore = _concrete.OnBeforeObjectSerialization;
            var originalOnAfter = _concrete.OnAfterObjectSerialization;
            _concrete.OnBeforeObjectSerialization = (x) => {
                OnBeforeObjectSerialization?.Invoke(x); // some callback the consummer might have set on this proxy
                onBeforeSerialization?.Invoke(x); // the callback set by the implementation (e.g. backing store)
                originalOnBefore?.Invoke(x); // some callback that might already be set on the target
            };
            _concrete.OnAfterObjectSerialization = (x) => {
                OnAfterObjectSerialization?.Invoke(x);
                onAfterSerialization?.Invoke(x);
                originalOnAfter?.Invoke(x);
            };
        }
        public void WriteStringValue(string key, string value) => _concrete.WriteStringValue(key, value);
        public void WriteBoolValue(string key, bool? value) => _concrete.WriteBoolValue(key, value);
        public void WriteIntValue(string key, int? value) => _concrete.WriteIntValue(key, value);
        public void WriteFloatValue(string key, float? value) => _concrete.WriteFloatValue(key, value);
        public void WriteDoubleValue(string key, double? value) => _concrete.WriteDoubleValue(key, value);
        public void WriteGuidValue(string key, Guid? value) => _concrete.WriteGuidValue(key, value);
        public void WriteDateTimeOffsetValue(string key, DateTimeOffset? value) => _concrete.WriteDateTimeOffsetValue(key, value);
        public void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values) => _concrete.WriteCollectionOfPrimitiveValues(key, values);
        public void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : IParsable => _concrete.WriteCollectionOfObjectValues(key, values);
        public Action<IParsable> OnBeforeObjectSerialization { get; set; }
        public Action<IParsable> OnAfterObjectSerialization { get; set; }
        public void WriteObjectValue<T>(string key, T value) where T : IParsable => _concrete.WriteObjectValue(key, value);
        public void WriteEnumValue<T>(string key, T? value) where T : struct, Enum => _concrete.WriteEnumValue(key, value);
        public void WriteAdditionalData(IDictionary<string, object> value) => _concrete.WriteAdditionalData(value);
        public Stream GetSerializedContent() => _concrete.GetSerializedContent();
        public void Dispose() => _concrete.Dispose();
    }
}
