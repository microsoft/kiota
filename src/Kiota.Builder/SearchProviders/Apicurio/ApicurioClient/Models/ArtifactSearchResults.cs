// <auto-generated/>
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
namespace ApiSdk.Models {
    /// <summary>
    /// Describes the response received when searching for artifacts.
    /// </summary>
    public class ArtifactSearchResults : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The artifacts returned in the result set.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<SearchedArtifact>? Artifacts { get; set; }
#nullable restore
#else
        public List<SearchedArtifact> Artifacts { get; set; }
#endif
        /// <summary>The total number of artifacts that matched the query that produced the result set (may be more than the number of artifacts in the result set).</summary>
        public int? Count { get; set; }
        /// <summary>
        /// Instantiates a new ArtifactSearchResults and sets the default values.
        /// </summary>
        public ArtifactSearchResults() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ArtifactSearchResults CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ArtifactSearchResults();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"artifacts", n => { Artifacts = n.GetCollectionOfObjectValues<SearchedArtifact>(SearchedArtifact.CreateFromDiscriminatorValue)?.ToList(); } },
                {"count", n => { Count = n.GetIntValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfObjectValues<SearchedArtifact>("artifacts", Artifacts);
            writer.WriteIntValue("count", Count);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
