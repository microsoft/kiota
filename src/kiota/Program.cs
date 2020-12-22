using System;
using System.IO;
using System.Net.Http;
using kiota.core;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Readers;

namespace kiota
{
    class Program
    {
        static void Main(string[] args)
        {
            var configruation = LoadConfiguration(args);
            var configObject = new GenerationConfiguration();
            configruation.Bind(configObject);
            Console.WriteLine($"{nameof(configObject.SomeArg)} equals {configObject.SomeArg}");
        }
        private static IConfiguration LoadConfiguration(string[] args) {
            var builder = new ConfigurationBuilder();
            return builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "KIOTA_")
                    .AddCommandLine(args)
                    .Build();
        }

        private static void GenerateSDK(string inputPath, string outputPath)
        {
            Stream input;
            if (inputPath.StartsWith("http"))
            {
                var httpClient = new HttpClient();
                input = httpClient.GetStreamAsync(inputPath).GetAwaiter().GetResult();
            }
            else
            {
                input = new FileStream(inputPath, FileMode.Open);
            }

            // Parse OpenAPI Input
            var reader = new OpenApiStreamReader();
            var doc = reader.Read(input, out var diag);
            // TODO: Check for errors

            // Generate Code Model
            var root = KiotaBuilder.Generate(doc);

            // Render source output
            var outfile = new FileStream(outputPath, FileMode.Create);
            var renderer = new CSharpRenderer();
            renderer.Render(root, outfile);
            outfile.Close();
        }
    }
}
