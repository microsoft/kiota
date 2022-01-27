// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Kiota.Abstractions.Authentication
{
    /// <summary>
    /// Validator for handling allowed hosts for authentication
    /// </summary>
    public class AllowedHostsValidator
    {
        private HashSet<string> _allowedHosts;

        /// <summary>
        /// The <see cref="AllowedHostsValidator"/> constructor
        /// </summary>
        public AllowedHostsValidator(): this(new HashSet<string>(StringComparer.OrdinalIgnoreCase))
        {
        }

        /// <summary>
        /// The <see cref="AllowedHostsValidator"/> constructor
        /// </summary>
        /// <param name="validHosts"> Collection of valid Hosts</param>
        public AllowedHostsValidator(IEnumerable<string> validHosts)
        {
            _allowedHosts = new HashSet<string>(validHosts, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets/Sets the collection of allowed hosts for the configurator
        /// </summary>
        public IEnumerable<string> AllowedHosts
        {
            get => _allowedHosts.ToArray();
            set
            {
                if(value is null) throw new ArgumentNullException(nameof(value));
                _allowedHosts = new HashSet<string>(value.Where(x => !string.IsNullOrEmpty(x)), StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Validates that the given Uri is valid
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to validate</param>
        /// <returns>
        /// true - if the host is in the <see cref="AllowedHosts"/>. If <see cref="AllowedHosts"/> is empty, it will return true for all urls.
        /// false - if the <see cref="AllowedHosts"/> is not empty and the host is not in the list
        /// </returns>
        public bool IsUrlHostValid(Uri uri)
        {
            return !_allowedHosts.Any() || _allowedHosts.Contains(uri.Host);
        }
    }
}
