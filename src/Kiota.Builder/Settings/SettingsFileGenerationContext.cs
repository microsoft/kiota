using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Kiota.Builder.Lock;

namespace Kiota.Builder.Settings;

[JsonSerializable(typeof(SettingsFile))]
internal partial class SettingsFileGenerationContext : JsonSerializerContext
{
}
