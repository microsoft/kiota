using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Store {
    public interface IBackingStore {
        T Get<T>(string key);
        void Set<T>(string key, T value);
        IEnumerable<KeyValuePair<string, object>> Enumerate();
        string Subscribe(Action<string, object, object> callback);
        void Unsubscribe(string subscriptionId);
        void Clear();
        bool InitializationCompleted { get; set; }
        bool ReturnOnlyChangedValues {get; set;}
    }
}
