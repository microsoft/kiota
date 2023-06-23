using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    public class ReactionRollup : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The confused property</summary>
        public int? Confused
        {
            get; set;
        }
        /// <summary>The eyes property</summary>
        public int? Eyes
        {
            get; set;
        }
        /// <summary>The heart property</summary>
        public int? Heart
        {
            get; set;
        }
        /// <summary>The hooray property</summary>
        public int? Hooray
        {
            get; set;
        }
        /// <summary>The laugh property</summary>
        public int? Laugh
        {
            get; set;
        }
        /// <summary>The minus_1 property</summary>
        public int? Minus1
        {
            get; set;
        }
        /// <summary>The plus_1 property</summary>
        public int? Plus1
        {
            get; set;
        }
        /// <summary>The rocket property</summary>
        public int? Rocket
        {
            get; set;
        }
        /// <summary>The total_count property</summary>
        public int? TotalCount
        {
            get; set;
        }
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url
        {
            get; set;
        }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>
        /// Instantiates a new reactionRollup and sets the default values.
        /// </summary>
        public ReactionRollup()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ReactionRollup CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ReactionRollup();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"confused", n => { Confused = n.GetIntValue(); } },
                {"eyes", n => { Eyes = n.GetIntValue(); } },
                {"heart", n => { Heart = n.GetIntValue(); } },
                {"hooray", n => { Hooray = n.GetIntValue(); } },
                {"laugh", n => { Laugh = n.GetIntValue(); } },
                {"-1", n => { Minus1 = n.GetIntValue(); } },
                {"+1", n => { Plus1 = n.GetIntValue(); } },
                {"rocket", n => { Rocket = n.GetIntValue(); } },
                {"total_count", n => { TotalCount = n.GetIntValue(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteIntValue("confused", Confused);
            writer.WriteIntValue("eyes", Eyes);
            writer.WriteIntValue("heart", Heart);
            writer.WriteIntValue("hooray", Hooray);
            writer.WriteIntValue("laugh", Laugh);
            writer.WriteIntValue("-1", Minus1);
            writer.WriteIntValue("+1", Plus1);
            writer.WriteIntValue("rocket", Rocket);
            writer.WriteIntValue("total_count", TotalCount);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
