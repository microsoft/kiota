using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class Topics415Error : ApiException, IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The documentation_url property</summary>
        public string Documentation_url { get; set; }
        /// <summary>The message property</summary>
        public string Message { get; set; }
        /// <summary>
        /// Instantiates a new topics415Error and sets the default values.
        /// </summary>
        public Topics415Error() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Topics415Error CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Topics415Error();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"documentation_url", n => { Documentation_url = n.GetStringValue(); } },
                {"message", n => { Message = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("documentation_url", Documentation_url);
            writer.WriteStringValue("message", Message);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
