﻿using System.Text.Json.Nodes;

namespace Kiota.Builder.OpenApiExtensions;

public class OpenApiPrivacyPolicyUrlExtension : OpenApiSimpleStringExtension
{
    public static string Name => "x-privacy-policy-url";
    public string? Privacy
    {
        get; set;
    }
    protected override string? ValueSelector => Privacy;
    public static OpenApiPrivacyPolicyUrlExtension Parse(JsonNode source)
    {
        return new OpenApiPrivacyPolicyUrlExtension
        {
            Privacy = ParseString(source)
        };
    }
}
