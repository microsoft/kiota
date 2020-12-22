using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace kiota.core
{
    public class KiotaBuilder
    {
        private ILogger<KiotaBuilder> logger;
        public KiotaBuilder(ILogger<KiotaBuilder> logger)
        {
            this.logger = logger;
        }
        private ILanguageRenderer GetRenderer(GenerationLanguage language)
        {
            switch (language)
            {
                case GenerationLanguage.CSharp:
                    return new CSharpRenderer();
                default:
                    throw new ArgumentException($"Unknown language {language}");
            } 
        }

        public async Task GenerateSDK(GenerationConfiguration config)
        {
            string inputPath = config.OpenAPIFilePath;

            Stream input;
            if (inputPath.StartsWith("http"))
            {
                using var httpClient = new HttpClient();
                input = await httpClient.GetStreamAsync(inputPath);
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
            var root = Generate(doc);
          
            // Render source output
            var renderer = GetRenderer(config.Language);
            renderer.Render(root, config);
            input?.Close();
        }



        public RequestBuilder Generate(OpenApiDocument doc)
        {
            RequestBuilder root = null;

            var paths = doc?.Paths;
            if (paths != null)
            {
                root = new RequestBuilder("/");

                foreach (var path in paths)
                {
                    Attach(root, path.Key, path.Value);
                }
            }
            return root;
        }

        private RequestBuilder Attach(RequestBuilder root, string path, OpenApiPathItem pathItem)
        {
            if (path.StartsWith("/"))  // remove leading slash
            {
                path = path[1..];
            }
            var segments = path.Split('/');
            return Attach(root, segments, pathItem, path);
        }

        private RequestBuilder Attach(RequestBuilder current, IEnumerable<string> segments, OpenApiPathItem pathItem, string path)
        {

            var segment = segments.FirstOrDefault();
            if (string.IsNullOrEmpty(segment))
            {
                current.PathItem = pathItem;
                current.Path = path;
                return current;
            }

            // If the child segment has already been defined, then insert into it
            if (current.Children.ContainsKey(segment))
            {
                return Attach(current.Children[segment], segments.Skip(1), pathItem, path);
            }
            else
            {
                var node = new RequestBuilder(segment);
                current.Children[segment] = node;
                return Attach(node, segments.Skip(1), pathItem, path);
            }
        }

    }
}
