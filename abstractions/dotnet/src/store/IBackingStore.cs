// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Store
{
    /// <summary>
    /// Stores model information in a different location than the object properties. Implementations can provide dirty tracking capabilities, caching capabilities or integration with 3rd party stores.
    /// </summary>
    public interface IBackingStore
    {
        /// <summary>Gets a value from the backing store based on its key. Returns null if the value hasn't changed and "ReturnOnlyChangedValues" is true.</summary>
        /// <returns>The value from the backing store.</returns>
        /// <param name="key">The key to lookup the backing store with.</param>
        T Get<T>(string key);
        /// <summary>
        /// Sets or updates the stored value for the given key.
        /// Will trigger subscriptions callbacks.
        /// </summary>
        /// <param name="key">The key to store and retrieve the information.</param>
        /// <param name="value">The value to be stored.</param>
        void Set<T>(string key, T value);
        /// <summary>Enumerates all the values stored in the backing store. Values will be filtered if "ReturnOnlyChangedValues" is true.</summary>
        /// <returns>The values available in the backing store.</returns>
        IEnumerable<KeyValuePair<string, object>> Enumerate();
        /// <summary>
        /// Enumerates the keys for all values that changed to null.
        /// </summary>
        /// <returns>The keys for all values that changed to null.</returns>
        IEnumerable<string> EnumerateKeysForValuesChangedToNull();
        /// <summary>
        /// Creates a subscription to any data change happening.
        /// </summary>
        /// <param name="callback">Callback to be invoked on data changes where the first parameter is the data key, the second the previous value and the third the new value.</param>
        /// <returns>The subscription Id to use when removing the subscription</returns>
        string Subscribe(Action<string, object, object> callback);
        /// <summary>
        /// Creates a subscription to any data change happening, allowing to specify the subscription Id.
        /// </summary>
        /// <param name="callback">Callback to be invoked on data changes where the first parameter is the data key, the second the previous value and the third the new value.</param>
        /// <param name="subscriptionId">The subscription Id to use.</param>
        void Subscribe(Action<string, object, object> callback, string subscriptionId);
        /// <summary>
        /// Removes a subscription from the store based on its subscription id.
        /// </summary>
        /// <param name="subscriptionId">The Id of the subscription to remove.</param>
        void Unsubscribe(string subscriptionId);
        /// <summary>
        /// Clears the data stored in the backing store. Doesn't trigger any subscription.
        /// </summary>
        void Clear();
        /// <value>Whether the initialization of the object and/or the initial deserialization has been completed to track whether objects have changed.</value>
        bool InitializationCompleted { get; set; }
        /// <value>Whether to return only values that have changed since the initialization of the object when calling the Get and Enumerate methods.</value>
        bool ReturnOnlyChangedValues { get; set; }
    }
}
