using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization {
    public interface IParsable<T> where T : class {
        IDictionary<string, Action<T, IParseNode>> DeserializeFields
        {
            get;
        }

        void Serialize(ISerializationWriter writer);

        IDictionary<string, object> AdditionalData { get; }
    }
}
