using System;

namespace Kiota.Builder.CodeDOM;

public record DeprecationInformation(string? Description, DateTimeOffset? Date, DateTimeOffset? RemovalDate, string? Version, bool IsDeprecated = true);
