// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Kiota.Abstractions.Extensions
{
    /// <summary>
    /// The class for extension methods for <see cref="string"/> type
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Returns a string with the first letter lowered.
        /// </summary>
        /// <param name="input">The string to lowercase.</param>
        /// <returns>The input string with the first letter lowered.</returns>
        public static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input[0])}{input[1..]}";
    }
}
