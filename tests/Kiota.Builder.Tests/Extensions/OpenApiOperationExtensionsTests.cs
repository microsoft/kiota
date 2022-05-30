using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Extensions.Tests {
    public class OpenApiOperationExtensionsTests {
        [Fact]
        public void GetsResponseSchema() {
            var operation = new OpenApiOperation{
                Responses = new() {
                    { "200", new() {
                        Content = new Dictionary<string, OpenApiMediaType> {
                            {"application/json", new() {
                                Schema = new()
                            }}
                        }
                    }}
                }
            };
            var operation2 = new OpenApiOperation{
                Responses = new() {
                    { "400", new() {
                        Content = new Dictionary<string, OpenApiMediaType> {
                            {"application/json", new() {
                                Schema = new()
                            }}
                        }
                    }}
                }
            };
            var operation3 = new OpenApiOperation{
                Responses = new() {
                    { "200", new() {
                        Content = new Dictionary<string, OpenApiMediaType> {
                            {"application/invalid", new() {
                                Schema = new()
                            }}
                        }
                    }}
                }
            };
            var defaultConfiguration = new GenerationConfiguration();
            Assert.NotNull(operation.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
            Assert.Null(operation2.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
            Assert.Null(operation3.GetResponseSchema(defaultConfiguration.StructuredMimeTypes));
        }

    }
}
