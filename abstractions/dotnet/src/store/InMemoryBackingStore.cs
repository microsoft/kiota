// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Kiota.Abstractions.Store
{
    /// <summary>
    ///     In-memory implementation of the backing store. Allows for dirty tracking of changes.
    /// </summary>
    public class InMemoryBackingStore : IBackingStore
    {
        private bool isInitializationComplete = true;
        /// <summary>
        /// Determines whether the backing store should only return changed values when queried.
        /// </summary>
        public bool ReturnOnlyChangedValues { get; set; }
        private readonly Dictionary<string, Tuple<bool, object>> store = new();
        private Dictionary<string, Action<string, object, object>> subscriptions = new();

        /// <summary>
        /// Gets the specified object with the given key from the store.
        /// </summary>
        /// <param name="key">The key to search with</param>
        /// <returns>An instance of <typeparam name="T"/></returns>
        public T Get<T>(string key)
        {
            if(string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if(store.TryGetValue(key, out var result))
                return ReturnOnlyChangedValues && result.Item1 || !ReturnOnlyChangedValues ? (T)result.Item2 : default;
            else
                return default;
        }

        /// <summary>
        /// Sets the specified object with the given key in the store.
        /// </summary>
        /// <param name="key">The key to use</param>
        /// <param name="value">The object value to store</param>
        public void Set<T>(string key, T value)
        {
            if(string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var valueToAdd = new Tuple<bool, object>(InitializationCompleted, value);
            Tuple<bool, object> oldValue = null;
            if(!store.TryAdd(key, valueToAdd))
            {
                oldValue = store[key];
                store[key] = valueToAdd;
            }
            foreach(var sub in subscriptions.Values)
                sub.Invoke(key, oldValue?.Item2, value);
        }

        /// <summary>
        /// Enumerate the values in the store based on the <see cref="ReturnOnlyChangedValues"/> configuration value.
        /// </summary>
        /// <returns>A collection of changed values or the whole store based on the <see cref="ReturnOnlyChangedValues"/> configuration value.</returns>
        public IEnumerable<KeyValuePair<string, object>> Enumerate()
        {
            return (ReturnOnlyChangedValues ? store.Where(x => !x.Value.Item1) : store)
                .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.Item2));
        }

        /// <summary>
        /// Enumerate the values in the store that have changed to null
        /// </summary>
        /// <returns>A collection of strings containing keys changed to null </returns>
        public IEnumerable<string> EnumerateKeysForValuesChangedToNull()
        {
            return store.Where(x => x.Value.Item1 && x.Value.Item2 == null).Select(x => x.Key);
        }

        /// <summary>
        /// Adds a callback to subscribe to events in the store
        /// </summary>
        /// <param name="callback">The callback to add</param>
        /// <returns>The id of the subscription</returns>
        public string Subscribe(Action<string, object, object> callback)
        {
            var id = Guid.NewGuid().ToString();
            Subscribe(callback, id);
            return id;
        }

        /// <summary>
        /// Adds a callback to subscribe to events in the store with the given subscription id
        /// </summary>
        /// <param name="callback">The callback to add</param>
        /// <param name="subscriptionId">The subscription id to use for subscription</param>
        public void Subscribe(Action<string, object, object> callback, string subscriptionId)
        {
            if(string.IsNullOrEmpty(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));
            if(callback == null)
                throw new ArgumentNullException(nameof(callback));
            subscriptions.Add(subscriptionId, callback);
        }

        /// <summary>
        /// De-register the callback with the given subscriptionId
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription to de-register </param>
        public void Unsubscribe(string subscriptionId)
        {
            store.Remove(subscriptionId);
        }
        /// <summary>
        /// Clears the store
        /// </summary>
        public void Clear()
        {
            store.Clear();
        }
        /// <summary>
        /// Flag to show the initialization status of the store.
        /// </summary>
        public bool InitializationCompleted
        {
            get { return isInitializationComplete; }
            set
            {
                isInitializationComplete = value;
                foreach(var entry in store)
                    store[entry.Key] = new(!value, entry.Value.Item2);
            }
        }
    }
}
