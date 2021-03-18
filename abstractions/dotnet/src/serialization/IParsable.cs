using System;
using System.Collections.Generic;

namespace Kiota.Abstractions.Serialization {
    public interface IParsable<T> where T : class {
        IDictionary<string, Action<T, IParseNode>> DeserializeFields
        {
            get;
        }

        IDictionary<string, Action<T, IParseNode>> SerializeFields
        {
            get;
        }
    }
}
