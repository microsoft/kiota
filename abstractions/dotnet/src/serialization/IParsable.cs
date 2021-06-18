using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface IParsable {
        IDictionary<string, Action<T, IParseNode>> GetFieldDeserializers<T>();
        void Serialize(ISerializationWriter writer);

        IDictionary<string, object> AdditionalData { get; set; }
    }
}
