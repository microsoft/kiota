#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public sealed class YamlToCSharpTests : IDisposable
{
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private readonly HttpClient _httpClient = new();

    public const string NullableInOpenApi3 = """
                                             {
                                               "openapi": "3.1.1",
                                               "info": {
                                                 "title": "WebApplication2 | v1",
                                                 "version": "1.0.0"
                                               },
                                               "servers": [
                                                 {
                                                   "url": "http://localhost:5088/"
                                                 }
                                               ],
                                               "paths": {
                                                 "/Sample": {
                                                   "get": {
                                                     "tags": [
                                                       "Sample"
                                                     ],
                                                     "operationId": "GetWeatherForecast",
                                                     "responses": {
                                                       "200": {
                                                         "description": "OK",
                                                         "content": {
                                                           "text/plain": {
                                                             "schema": {
                                                               "$ref": "#/components/schemas/Sample"
                                                             }
                                                           },
                                                           "application/json": {
                                                             "schema": {
                                                               "$ref": "#/components/schemas/Sample"
                                                             }
                                                           },
                                                           "text/json": {
                                                             "schema": {
                                                               "$ref": "#/components/schemas/Sample"
                                                             }
                                                           }
                                                         }
                                                       }
                                                     }
                                                   }
                                                 }
                                               },
                                               "components": {
                                                 "schemas": {
                                                   "Nested": {
                                                     "required": [
                                                       "value"
                                                     ],
                                                     "type": "object",
                                                     "properties": {
                                                       "value": {
                                                         "type": "string"
                                                       }
                                                     }
                                                   },
                                                   "Sample": {
                                                     "required": [
                                                       "immediate",
                                                       "nestedNonNullable",
                                                       "nestedNullable"
                                                     ],
                                                     "type": "object",
                                                     "properties": {
                                                       "immediate": {
                                                         "type": "string"
                                                       },
                                                       "nestedNonNullable": {
                                                         "$ref": "#/components/schemas/Nested"
                                                       },
                                                       "nestedNullable": {
                                                         "oneOf": [
                                                           {
                                                             "type": "null"
                                                           },
                                                           {
                                                             "$ref": "#/components/schemas/Nested"
                                                           }
                                                         ]
                                                       }
                                                     }
                                                   }
                                                 }
                                               },
                                               "tags": [
                                                 {
                                                   "name": "Sample"
                                                 }
                                               ]
                                             }
                                             """;

    public const string NullableInOpenApi3_Models_Sample = """
                                             // <auto-generated/>
                                             #pragma warning disable CS0618
                                             using Microsoft.Kiota.Abstractions.Extensions;
                                             using Microsoft.Kiota.Abstractions.Serialization;
                                             using System.Collections.Generic;
                                             using System.IO;
                                             using System;
                                             namespace ApiSdk.Models
                                             {
                                                 [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
                                                 #pragma warning disable CS1591
                                                 public partial class Sample : IAdditionalDataHolder, IParsable
                                                 #pragma warning restore CS1591
                                                 {
                                                     /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
                                                     public IDictionary<string, object> AdditionalData { get; set; }
                                                     /// <summary>The immediate property</summary>
                                             #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                                             #nullable enable
                                                     public string? Immediate { get; set; }
                                             #nullable restore
                                             #else
                                                     public string Immediate { get; set; }
                                             #endif
                                                     /// <summary>The nestedNonNullable property</summary>
                                             #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                                             #nullable enable
                                                     public global::ApiSdk.Models.Nested? NestedNonNullable { get; set; }
                                             #nullable restore
                                             #else
                                                     public global::ApiSdk.Models.Nested NestedNonNullable { get; set; }
                                             #endif
                                                     /// <summary>The nestedNullable property</summary>
                                             #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
                                             #nullable enable
                                                     public global::ApiSdk.Models.Nested? NestedNullable { get; set; }
                                             #nullable restore
                                             #else
                                                     public global::ApiSdk.Models.Nested NestedNullable { get; set; }
                                             #endif
                                                     /// <summary>
                                                     /// Instantiates a new <see cref="global::ApiSdk.Models.Sample"/> and sets the default values.
                                                     /// </summary>
                                                     public Sample()
                                                     {
                                                         AdditionalData = new Dictionary<string, object>();
                                                     }
                                                     /// <summary>
                                                     /// Creates a new instance of the appropriate class based on discriminator value
                                                     /// </summary>
                                                     /// <returns>A <see cref="global::ApiSdk.Models.Sample"/></returns>
                                                     /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
                                                     public static global::ApiSdk.Models.Sample CreateFromDiscriminatorValue(IParseNode parseNode)
                                                     {
                                                         if(ReferenceEquals(parseNode, null)) throw new ArgumentNullException(nameof(parseNode));
                                                         return new global::ApiSdk.Models.Sample();
                                                     }
                                                     /// <summary>
                                                     /// The deserialization information for the current model
                                                     /// </summary>
                                                     /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
                                                     public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
                                                     {
                                                         return new Dictionary<string, Action<IParseNode>>
                                                         {
                                                             { "immediate", n => { Immediate = n.GetStringValue(); } },
                                                             { "nestedNonNullable", n => { NestedNonNullable = n.GetObjectValue<global::ApiSdk.Models.Nested>(global::ApiSdk.Models.Nested.CreateFromDiscriminatorValue); } },
                                                             { "nestedNullable", n => { NestedNullable = n.GetObjectValue<global::ApiSdk.Models.Nested>(global::ApiSdk.Models.Nested.CreateFromDiscriminatorValue); } },
                                                         };
                                                     }
                                                     /// <summary>
                                                     /// Serializes information the current object
                                                     /// </summary>
                                                     /// <param name="writer">Serialization writer to use to serialize this model</param>
                                                     public virtual void Serialize(ISerializationWriter writer)
                                                     {
                                                         if(ReferenceEquals(writer, null)) throw new ArgumentNullException(nameof(writer));
                                                         writer.WriteStringValue("immediate", Immediate);
                                                         writer.WriteObjectValue<global::ApiSdk.Models.Nested>("nestedNonNullable", NestedNonNullable);
                                                         writer.WriteObjectValue<global::ApiSdk.Models.Nested>("nestedNullable", NestedNullable);
                                                         writer.WriteAdditionalData(AdditionalData);
                                                     }
                                                 }
                                             }
                                             #pragma warning restore CS0618
                                             
                                             """;


    [Theory]
    [InlineData(NullableInOpenApi3, new[] {"Models/Sample.cs", NullableInOpenApi3_Models_Sample})]
    public async Task CreateOpenApiDocumentWithResultAsync_ReturnsDiagnostics(string input,
        string[] expectedData)
    {
        if (expectedData.Length % 2 != 0)
            Assert.Fail("Invalid test data");
        var expectedList = expectedData.Chunk(2).Select(e => (fileName: e[0], expected: e[1])).ToList();

        string? tempInputFile = null;
        string? tempOutputDirectory = null;
        try
        {
            tempInputFile = Path.GetTempFileName();
            tempOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempOutputDirectory);
            await File.WriteAllTextAsync(tempInputFile, input);
            var mockLogger = new Mock<ILogger<KiotaBuilder>>();
            var builder = new KiotaBuilder(mockLogger.Object,
                new GenerationConfiguration
                {
                    ClientClassName = "Graph",
                    OpenAPIFilePath = tempInputFile,
                    OutputPath = tempOutputDirectory,
                    Language = GenerationLanguage.CSharp
                }, _httpClient);
            var result = await builder.GenerateClientAsync(default);
            Assert.True(result);
            foreach (var (fileName, expected) in expectedList)
            {
                var contents = await File.ReadAllTextAsync(Path.Combine(tempOutputDirectory, fileName));
                Assert.Equal(expected, contents);
            }
        }
        finally
        {
            if (tempInputFile is not null && File.Exists(tempInputFile))
                File.Delete(tempInputFile);
            if (tempOutputDirectory is not null && Directory.Exists(tempOutputDirectory))
                Directory.Delete(tempOutputDirectory, true);
        }
    }
}
