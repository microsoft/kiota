// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    ///   Service responsible for translating abstract Request Info into concrete native HTTP requests.
    /// </summary>
    public interface IRequestAdapter
    {
        /// <summary>
        ///  Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
        /// </summary>
        /// <param name="backingStoreFactory">The backing store factory to use.</param>
        void EnableBackingStore(IBackingStoreFactory backingStoreFactory);
        /// <summary>
        /// Gets the serialization writer factory currently in use for the HTTP core service.
        /// </summary>
        ISerializationWriterFactory SerializationWriterFactory { get; }
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="factory">The factory of the response model to deserialize the response into.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the requests.</param>
        /// <returns>The deserialized response model.</returns>
        Task<ModelType> SendAsync<ModelType>(RequestInformation requestInfo, ParsableFactory<ModelType> factory, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable;
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="factory">The factory of the response model to deserialize the response into.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the requests.</param>
        /// <returns>The deserialized response model collection.</returns>
        Task<IEnumerable<ModelType>> SendCollectionAsync<ModelType>(RequestInformation requestInfo, ParsableFactory<ModelType> factory, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable;
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the requests.</param>
        /// <returns>The deserialized primitive response model.</returns>
        Task<ModelType> SendPrimitiveAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default);
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the requests.</param>
        /// <returns>The deserialized primitive response model collection.</returns>
        Task<IEnumerable<ModelType>> SendPrimitiveCollectionAsync<ModelType>(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default);
        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation with no return content.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="responseHandler">The response handler to use for the HTTP request instead of the default handler.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the requests.</param>
        /// <returns>A Task to await completion.</returns>
        Task SendNoContentAsync(RequestInformation requestInfo, IResponseHandler responseHandler = default, Dictionary<string, Func<IParsable>> errorMapping = default, CancellationToken cancellationToken = default);
        /// <summary>
        /// The base url for every request.
        /// </summary>
        string BaseUrl { get; set; }
    }
}
