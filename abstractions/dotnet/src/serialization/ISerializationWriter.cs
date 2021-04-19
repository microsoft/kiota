using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Kiota.Abstractions.Serialization {
    public interface ISerializationWriter : IDisposable, IAsyncDisposable {
        void WriteStringValue(string key, string value);
        void WriteBoolValue(string key, bool value);
        void WriteIntValue(string key, int value);
        void WriteFloatValue(string key, float value);
        void WriteDoubleValue(string key, double value);
        void WriteGuidValue(string key, Guid value);
        void WriteDateTimeOffsetValue(string key, DateTimeOffset value);
        void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values);
        void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : class, IParsable<T>, new();
        void WriteObjectValue<T>(string key, T value) where T : class, IParsable<T>, new();
        Task<Stream> GetSerializedContent();
    }
}
