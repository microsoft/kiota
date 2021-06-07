using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Kiota.Abstractions.Store {
    public class InMemoryBackingStore : IBackingStore {
        private bool isInitializationComplete;
        public bool ReturnOnlyChangedValues {get; set;}
        private readonly Dictionary<string, KeyValuePair<bool, object>> store = new();
        private Dictionary<string, Action<string, object, object>> subscriptions = new();
        public T Get<T>(string key) {
            if(string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if(store.TryGetValue(key, out var result))
                return (T)result.Value;
            else
                return default;
        }
        public void Set<T>(string key, T value) {
            if(string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var valueToAdd = new KeyValuePair<bool, object>(InitilizationCompleted, value);
            object oldValue = null;
            if(!store.TryAdd(key, valueToAdd)) {
                oldValue = store[key];
                store[key] = valueToAdd;
            }
            foreach(var sub in subscriptions.Values)
                sub.Invoke(key, value, oldValue);
        }
        public IEnumerable<KeyValuePair<string, object>> Enumerate() {
            return (ReturnOnlyChangedValues ? store.Where(x => !x.Value.Key) : store)
                .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Value));
        }
        public string Subscribe(Action<string, object, object> callback) {
            var id = Guid.NewGuid().ToString();
            subscriptions.Add(id, callback);
            return id;
        }
        public void Unsubscribe(string subscriptionId) {
            store.Remove(subscriptionId);
        }
        public void Clear() {
            store.Clear();
        }
        public bool InitilizationCompleted { 
            get { return isInitializationComplete; } 
            set {
                isInitializationComplete = value;
                store.Values.ToList().ForEach(x => x.Key = !value);
            }
        }
    }
}
