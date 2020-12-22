using System;
using System.IO;
using kiota.core;
using Microsoft.Extensions.Configuration;

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
        private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
    }
}
