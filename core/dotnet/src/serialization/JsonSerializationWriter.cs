using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Kiota.Abstractions.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KiotaCore.Serialization {
    public class JsonSerializationWriter : ISerializationWriter, IDisposable, IAsyncDisposable {
        private readonly MemoryStream stream = new MemoryStream();
        public readonly Utf8JsonWriter writer;
        public JsonSerializationWriter()
        {
            writer = new Utf8JsonWriter(stream);
        }
        public async Task<Stream> GetSerializedContent() {
            await writer.FlushAsync();
            stream.Position = 0;
            return stream;
        }
        public void WriteStringValue(string key, string value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteStringValue(value);
        }
        public void WriteBoolValue(string key, bool value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteBooleanValue(value);
        }
        public void WriteIntValue(string key, int value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteNumberValue(value);
        }
        public void WriteFloatValue(string key, float value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteNumberValue(value);
        }
        public void WriteDoubleValue(string key, double value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteNumberValue(value);
        }
        public void WriteGuidValue(string key, Guid value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteStringValue(value);
        }
        public void WriteDateTimeOffsetValue(string key, DateTimeOffset value) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteStringValue(value);
        }
        public void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values) {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteStartArray();
            if(values?.Any() ?? false)
                foreach(var collectionValue in values) {
                    switch(collectionValue) {
                        case bool v:
                            writer.WriteBooleanValue(v);
                        break;
                        case string v:
                            writer.WriteStringValue(v);
                        break;
                        case Guid v:
                            writer.WriteStringValue(v);
                        break;
                        case DateTimeOffset v:
                            writer.WriteStringValue(v);
                        break;
                        case int v:
                            writer.WriteNumberValue(v);
                        break;
                        case float v:
                            writer.WriteNumberValue(v);
                        break;
                        case double v:
                            writer.WriteNumberValue(v);
                        break;
                        default:
                            throw new InvalidOperationException($"unknown type for serialization {collectionValue.GetType().FullName}");
                    }
                }
            writer.WriteEndArray();
        }
        public void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : class, IParsable<T>, new() {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteStartArray();
            if(values?.Any() ?? false)
                foreach(var item in values.Where(x => x != null)) {
                    writer.WriteStartObject();
                    item.Serialize(this);
                    writer.WriteEndObject();
                }
            writer.WriteEndArray();
        }
        public void WriteObjectValue<T>(string key, T value) where T : class, IParsable<T>, new() {
            if(value != null) {
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartObject();
                value.Serialize(this);
                writer.WriteEndObject();
            }
        }
        public void Dispose()
        {
            writer.Dispose();
        }
        public ValueTask DisposeAsync()
        {
            return writer.DisposeAsync();
        }
    }
}
