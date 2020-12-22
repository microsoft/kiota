using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace kiota.core
{
    public static class KiotaBuilder
    {
        public static void GenerateSDK(GenerationConfiguration config)
        {
            string inputPath = config.OpenAPIFilePath;
            string outputPath = config.OutputPath;

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



        public static RequestBuilder Generate(OpenApiDocument doc)
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

        private static RequestBuilder Attach(RequestBuilder root, string path, OpenApiPathItem pathItem)
        {
            if (path.StartsWith("/"))  // remove leading slash
            {
                path = path[1..];
            }
            var segments = path.Split('/');
            return Attach(root, segments, pathItem, path);
        }

        private static RequestBuilder Attach(RequestBuilder current, IEnumerable<string> segments, OpenApiPathItem pathItem, string path)
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
