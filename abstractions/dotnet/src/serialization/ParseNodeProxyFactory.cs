// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization
{
    /// <summary>
    /// Proxy factory that allows the composition of before and after callbacks on existing factories.
    /// </summary>
    public abstract class ParseNodeProxyFactory : IParseNodeFactory
    {
        /// <summary>
        /// The valid content type for the <see cref="ParseNodeProxyFactory"/> instance
        /// </summary>
        public string ValidContentType { get { return _concrete.ValidContentType; } }
        private readonly IParseNodeFactory _concrete;
        private readonly Action<IParsable> _onBefore;
        private readonly Action<IParsable> _onAfter;
        /// <summary>
        /// Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
        /// </summary>
        /// <param name="concrete">The concrete factory to wrap.</param>
        /// <param name="onBefore">The callback to invoke before the deserialization of any model object.</param>
        /// <param name="onAfter">The callback to invoke after the deserialization of any model object.</param>
        public ParseNodeProxyFactory(IParseNodeFactory concrete, Action<IParsable> onBefore, Action<IParsable> onAfter)
        {
            _concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            _onBefore = onBefore;
            _onAfter = onAfter;
        }
        /// <summary>
        /// Create a parse node from the given stream and content type.
        /// </summary>
        /// <param name="content">The stream to read the parse node from.</param>
        /// <param name="contentType">The content type of the parse node.</param>
        /// <returns>A parse node.</returns>
        public IParseNode GetRootParseNode(string contentType, Stream content)
        {
            var node = _concrete.GetRootParseNode(contentType, content);
            var originalBefore = node.OnBeforeAssignFieldValues;
            var originalAfter = node.OnAfterAssignFieldValues;
            node.OnBeforeAssignFieldValues = (x) =>
            {
                _onBefore?.Invoke(x);
                originalBefore?.Invoke(x);
            };
            node.OnAfterAssignFieldValues = (x) =>
            {
                _onAfter?.Invoke(x);
                originalAfter?.Invoke(x);
            };
            return node;
        }
    }
}
