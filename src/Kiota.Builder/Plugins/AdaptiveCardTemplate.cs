using System.IO;
using Microsoft.Extensions.Logging;

namespace Kiota.Builder.Plugins
{

    internal class AdaptiveCardTemplate
    {
        private readonly ILogger<KiotaBuilder> Logger;
        private readonly string AdaptiveCard;

        public AdaptiveCardTemplate(ILogger<KiotaBuilder> logger)
        {
            Logger = logger;
            AdaptiveCard = LoadEmbeddedResource("Kiota.Builder.Resources.AdaptiveCardTemplate.json");
        }

        private string LoadEmbeddedResource(string resourceName)
        {
            var assembly = typeof(AdaptiveCardTemplate).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Logger.LogCritical("Failed to load embedded resource: {ResourceName}", resourceName);
                throw new FileNotFoundException($"Resource {resourceName} not found.");
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Write(string filePath)
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(filePath, AdaptiveCard);
            }
            catch (IOException e)
            {
                Logger.LogCritical("Failed to add adaptive-card.json due to an IO error: {Message}", e.Message);
            }
        }
    }
}
