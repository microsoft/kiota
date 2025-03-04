﻿using System;
using System.Collections.Generic;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

using Microsoft.OpenApi.Models;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class OpenApiOperationExtensionsTests
{
    [Fact]
    public void GetsResponseSchema()
    {
        var operation = new OpenApiOperation
        {
            Responses = new() {
                { "200", new OpenApiResponse() {
                    Content = new Dictionary<string, OpenApiMediaType> {
                        {"application/json", new() {
                            Schema = new OpenApiSchema()
                        }}
                    }
                }}
            }
        };
        var operation2 = new OpenApiOperation
        {
            Responses = new() {
                { "400", new OpenApiResponse() {
                    Content = new Dictionary<string, OpenApiMediaType> {
                        {"application/json", new() {
                            Schema = new OpenApiSchema()
                        }}
                    }
                }}
            }
        };
        var operation3 = new OpenApiOperation
        {
            Responses = new() {
                { "200", new OpenApiResponse() {
                    Content = new Dictionary<string, OpenApiMediaType> {
                        {"application/invalid", new() {
                            Schema = new OpenApiSchema()
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
    [Fact]
    public void Defensive()
    {
        var source = new Dictionary<string, OpenApiMediaType>();
        Assert.Empty(source.GetValidSchemas(new() { "application/json" }));
        Assert.Throws<ArgumentNullException>(() => source.GetValidSchemas(null));
    }
}
