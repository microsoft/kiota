using System;

namespace Kiota.Builder.CodeDOM;

public record DeprecationInformation(string? Description, DateTimeOffset? Date = null, DateTimeOffset? RemovalDate = null, string? Version = "", bool IsDeprecated = true);
