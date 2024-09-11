﻿using System;
using Kiota.Builder.Validation;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Tests.Validation;

public class OpenApiSchemaComparerTests
{
    private readonly OpenApiSchemaComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.True(_comparer.Equals(new(), new()));
    }
    [Fact]
    public void DoesNotStackOverFlowOnCircularReferencesForEquals()
    {
        var schema = new OpenApiSchema
        {

        };
        schema.Properties.Add("test", schema);
        schema.AnyOf.Add(schema);
        var schema2 = new OpenApiSchema
        {

        };
        schema2.Properties.Add("test", schema2);
        schema2.AnyOf.Add(schema2);
        Assert.True(_comparer.Equals(schema, schema2));
    }
}
