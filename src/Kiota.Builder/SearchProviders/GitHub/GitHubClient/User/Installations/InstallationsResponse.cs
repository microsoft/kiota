﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.User.Installations;
public class InstallationsResponse : IAdditionalDataHolder, IParsable
{
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public List<Installation>? Installations
    {
        get; set;
    }
#nullable restore
#else
    public List<Installation> Installations { get; set; }
#endif
    public int? Total_count
    {
        get; set;
    }
    /// <summary>
    /// Instantiates a new installationsResponse and sets the default values.
    /// </summary>
    public InstallationsResponse()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static InstallationsResponse CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new InstallationsResponse();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"installations", n => { Installations = n.GetCollectionOfObjectValues<Installation>(Installation.CreateFromDiscriminatorValue)?.ToList(); } },
            {"total_count", n => { Total_count = n.GetIntValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteCollectionOfObjectValues<Installation>("installations", Installations);
        writer.WriteIntValue("total_count", Total_count);
        writer.WriteAdditionalData(AdditionalData);
    }
}
