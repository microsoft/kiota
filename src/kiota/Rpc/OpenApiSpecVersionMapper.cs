using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class OpenApiSpecVersionMapper
    {
        internal static OpenApiTreeSpecVersion? FromOpenApiSpecVersion(OpenApiSpecVersion? version)
        {
            if (version == null)
                return null;
            var specVersion = version switch
            {
                OpenApiSpecVersion.OpenApi2_0 => OpenApiTreeSpecVersion.OpenApi2_0,
                OpenApiSpecVersion.OpenApi3_0 => OpenApiTreeSpecVersion.OpenApi3_0,
                OpenApiSpecVersion.OpenApi3_1 => OpenApiTreeSpecVersion.OpenApi3_1,
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
            return specVersion;
        }
    }
}
