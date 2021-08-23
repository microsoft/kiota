// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization
{
    /// <summary>
    /// Defines the contract for a factory that creates parse nodes.
    /// </summary>
    public interface IParseNodeFactory
    {
        /// <summary>
        /// Returns the content type this factory's parse nodes can deserialize.
        /// </summary>
        string ValidContentType { get; }
        /// <summary>
        /// Create a parse node from the given stream and content type.
        /// </summary>
        /// <param name="content">The stream to read the parse node from.</param>
        /// <param name="contentType">The content type of the parse node.</param>
        /// <returns>A parse node.</returns>
        IParseNode GetRootParseNode(string contentType, Stream content);
    }
}
