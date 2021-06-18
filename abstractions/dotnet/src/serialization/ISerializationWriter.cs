using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface ISerializationWriter : IDisposable {
        void WriteStringValue(string key, string value);
        void WriteBoolValue(string key, bool? value);
        void WriteIntValue(string key, int? value);
        void WriteFloatValue(string key, float? value);
        void WriteDoubleValue(string key, double? value);
        void WriteGuidValue(string key, Guid? value);
        void WriteDateTimeOffsetValue(string key, DateTimeOffset? value);
        void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values);
        void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : IParsable;
        void WriteObjectValue<T>(string key, T value) where T : IParsable;
        void WriteEnumValue<T>(string key, T? value) where T : struct, Enum;
        void WriteAdditionalData(IDictionary<string, object> value);
        Stream GetSerializedContent();
        Action<IParsable> OnBeforeObjectSerialization { get; set; }
        Action<IParsable> OnAfterObjectSerialization { get; set; }
    }
}
