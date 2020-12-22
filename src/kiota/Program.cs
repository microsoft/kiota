using System;
using kiota.core;
using Microsoft.Extensions.Configuration;

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
    }
}
