using System;

namespace Kiota.Builder;
public static class Constants
{
    public const string DefaultOpenApiLabel = "default";
    public const string RawUrlParameterName = "request-raw-url";
    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(100);
    public const string TempDirectoryName = "kiota";
}
