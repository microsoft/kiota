using System;
using System.IO;
<<<<<<< HEAD
using System.Net.Http;
=======
>>>>>>> d0b21302829b9a2b0b32c9f56110233417b50868
using kiota.core;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Readers;

namespace kiota
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = LoadConfiguration(args);
            
            Console.WriteLine($"{nameof(configuration.OpenAPIFilePath)} equals {configuration.OpenAPIFilePath}");
        }
        private static GenerationConfiguration LoadConfiguration(string[] args) {
            var builder = new ConfigurationBuilder();
            var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "KIOTA_")
                    .AddCommandLine(args)
                    .Build();
            var configObject = new GenerationConfiguration();
            configuration.Bind(configObject);
            configObject.OpenAPIFilePath = GetAbsolutePath(configObject.OpenAPIFilePath);
            configObject.OutputPath = GetAbsolutePath(configObject.OutputPath);
            return configObject;
        }
<<<<<<< HEAD

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
=======
        private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
>>>>>>> d0b21302829b9a2b0b32c9f56110233417b50868
    }
}
