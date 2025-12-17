using System.IO;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Plugins
{

    internal partial class AdaptiveCardTemplate
    {
        private readonly ILogger<KiotaBuilder> Logger;
        private readonly string? AdaptiveCard;

        public AdaptiveCardTemplate(ILogger<KiotaBuilder> logger)
        {
            Logger = logger;
            AdaptiveCard = LoadEmbeddedResource("Kiota.Builder.Resources.AdaptiveCardTemplate.json");
        }

        private string? LoadEmbeddedResource(string resourceName)
        {
            var assembly = typeof(AdaptiveCardTemplate).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            LogFailedToLoadEmbeddedResource(resourceName);
            return null;
        }

        [LoggerMessage(Level = LogLevel.Critical, Message = "Failed to load embedded resource: {ResourceName}")]
        private partial void LogFailedToLoadEmbeddedResource(string resourceName);

        public void Write(string filePath)
        {
            try
            {
                if (AdaptiveCard is null)
                {
                    throw new IOException("Failed to load the adaptive card from the embedded resource");
                }

                string? directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(filePath, AdaptiveCard);
            }
            catch (IOException e)
            {
                throw new IOException($"Failed to write to file {filePath}", e);
            }
        }
    }
}
