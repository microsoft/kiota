using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Store {
    public interface IBackingStore {
        T Get<T>(string key);
        void Set<T>(string key, T value);
        IEnumerable<KeyValuePair<string, object>> Enumerate(bool includeUnchanged = false);
        string Subscribe(Action<string, object, object> callback);
        void Unsubscribe(string subscriptionId);
        void Clear();
        void SetInitilizationCompleted();
    }
}
