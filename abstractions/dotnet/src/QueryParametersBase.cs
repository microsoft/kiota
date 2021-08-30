// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    /// The base implementation of the Query Parameters 
    /// </summary>
    public abstract class QueryParametersBase
    {
        /// <summary>
        /// Vanity method to add the query parameters to the request query parameters dictionary.
        /// </summary>
        public void AddQueryParameters(IDictionary<string, object> target)
        {
            if(target == null) throw new ArgumentNullException(nameof(target));
            foreach(var property in this.GetType()
                                        .GetProperties()
                                        .Where(x => !target.ContainsKey(x.Name)))
            {
                target.Add(property.Name, property.GetValue(this));
            }
        }
    }
}
