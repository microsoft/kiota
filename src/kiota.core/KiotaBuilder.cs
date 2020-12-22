using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public static class KiotaBuilder
    {
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
