// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Json
{
    /// <summary>
    /// The <see cref="IParseNodeFactory"/> implementation for json content types
    /// </summary>
    public class JsonParseNodeFactory : IParseNodeFactory
    {
        /// <summary>
        /// The valid content type for json
        /// </summary>
        public string ValidContentType { get; } = "application/json";

        /// <summary>
        /// Gets the root <see cref="IParseNode"/> of the json to be read.
        /// </summary>
        /// <param name="contentType">The content type of the stream to be parsed</param>
        /// <param name="content">The <see cref="Stream"/> containing json to parse.</param>
        /// <returns>An instance of <see cref="IParseNode"/> for json manipulation</returns>
        public IParseNode GetRootParseNode(string contentType, Stream content)
        {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");

            _ = content ?? throw new ArgumentNullException(nameof(content));

            using var jsonDocument = JsonDocument.Parse(content);
            return new JsonParseNode(jsonDocument.RootElement.Clone());
        }
    }
}
