// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
using Tavis.UriTemplates;

namespace Microsoft.Kiota.Abstractions
{
    /// <summary>
    ///     This class represents an abstract HTTP request.
    /// </summary>
    public class RequestInformation
    {
        private Uri _rawUri;
        /// <summary>
        ///  The URI of the request.
        /// </summary>
        public Uri URI {
            set {
                if(value == null)
                    throw new ArgumentNullException(nameof(value));
                QueryParameters.Clear();
                UrlTemplateParameters.Clear();
                _rawUri = value;
            }
            get {
                if(_rawUri != null)
                    return _rawUri;
                else if(UrlTemplateParameters.TryGetValue("request-raw-url", out var rawUrl)) {
                    URI = new Uri(rawUrl);
                    return _rawUri;
                }
                else
                {
                    var parsedUrlTemplate = new UriTemplate(UrlTemplate);
                    foreach(var urlTemplateParameter in UrlTemplateParameters)
                        parsedUrlTemplate.SetParameter(urlTemplateParameter.Key, urlTemplateParameter.Value);

                    foreach(var queryStringParameter in QueryParameters)
                        if(queryStringParameter.Value != null)
                            parsedUrlTemplate.SetParameter(queryStringParameter.Key, queryStringParameter.Value);
                    return new Uri(parsedUrlTemplate.Resolve());
                }
            }
        }
        /// <summary>
        /// The Url template for the current request.
        /// </summary>
        public string UrlTemplate { get; set; }
        /// <summary>
        /// The parameters to use for the URL template when generating the URI in addition to the query parameters.
        /// </summary>
        public IDictionary<string, string> UrlTemplateParameters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        ///  The <see cref="HttpMethod">HTTP method</see> of the request.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }
        /// <summary>
        /// The Query Parameters of the request.
        /// </summary>
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The Request Headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The Request Body.
        /// </summary>
        public Stream Content { get; set; }
        private Dictionary<string, IRequestOption> _requestOptions = new Dictionary<string, IRequestOption>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
        /// </summary>
        public IEnumerable<IRequestOption> RequestOptions { get { return _requestOptions.Values; } }
        /// <summary>
        /// Adds an option to the request.
        /// </summary>
        /// <param name="options">The option to add.</param>
        public void AddRequestOptions(params IRequestOption[] options)
        {
            if(!(options?.Any() ?? false)) return; // it's a no-op if there are no options and this avoid having to check in the code gen.
            foreach(var option in options.Where(x => x != null))
                if(!_requestOptions.TryAdd(option.GetType().FullName, option))
                    _requestOptions[option.GetType().FullName] = option;
        }
        /// <summary>
        /// Removes given options from the current request.
        /// </summary>
        /// <param name="options">Options to remove.</param>
        public void RemoveRequestOptions(params IRequestOption[] options)
        {
            if(!options?.Any() ?? false) throw new ArgumentNullException(nameof(options));
            foreach(var optionName in options.Where(x => x != null).Select(x => x.GetType().FullName))
                _requestOptions.Remove(optionName);
        }
        private const string BinaryContentType = "application/octet-stream";
        private const string ContentTypeHeader = "Content-Type";
        /// <summary>
        /// Sets the request body to a binary stream.
        /// </summary>
        /// <param name="content">The binary stream to set as a body.</param>
        public void SetStreamContent(Stream content)
        {
            Content = content;
            Headers.Add(ContentTypeHeader, BinaryContentType);
        }
        /// <summary>
        /// Sets the request body from a model with the specified content type.
        /// </summary>
        /// <param name="requestAdapter">The core service to get the serialization writer from.</param>
        /// <param name="items">The models to serialize.</param>
        /// <param name="contentType">The content type to set.</param>
        /// <typeparam name="T">The model type to serialize.</typeparam>
        public void SetContentFromParsable<T>(IRequestAdapter requestAdapter, string contentType, params T[] items) where T : IParsable
        {
            if(string.IsNullOrEmpty(contentType)) throw new ArgumentNullException(nameof(contentType));
            if(requestAdapter == null) throw new ArgumentNullException(nameof(requestAdapter));
            if(items == null || !items.Any()) throw new InvalidOperationException($"{nameof(items)} cannot be null or empty");

            using var writer = requestAdapter.SerializationWriterFactory.GetSerializationWriter(contentType);
            if(items.Count() == 1)
                writer.WriteObjectValue(null, items[0]);
            else
                writer.WriteCollectionOfObjectValues(null, items);
            Headers.Add(ContentTypeHeader, contentType);
            Content = writer.GetSerializedContent();
        }
    }
}
