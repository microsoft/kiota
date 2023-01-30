using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
/// <summary>
/// Validation Error
/// </summary>
public class ValidationError : ApiException, IAdditionalDataHolder, IParsable
{
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Documentation_url
    {
        get; set;
    }
#nullable restore
#else
    public string Documentation_url { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public List<ValidationError_errors>? Errors
    {
        get; set;
    }
#nullable restore
#else
    public List<ValidationError_errors> Errors { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public new string? Message
    {
        get; set;
    }
#nullable restore
#else
    public new string Message { get; set; }
#endif
    /// <summary>
    /// Instantiates a new ValidationError and sets the default values.
    /// </summary>
    public ValidationError()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static ValidationError CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new ValidationError();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"documentation_url", n => { Documentation_url = n.GetStringValue(); } },
            {"errors", n => { Errors = n.GetCollectionOfObjectValues<ValidationError_errors>(ValidationError_errors.CreateFromDiscriminatorValue)?.ToList(); } },
            {"message", n => { Message = n.GetStringValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteStringValue("documentation_url", Documentation_url);
        writer.WriteCollectionOfObjectValues<ValidationError_errors>("errors", Errors);
        writer.WriteStringValue("message", Message);
        writer.WriteAdditionalData(AdditionalData);
    }
}
