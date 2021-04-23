using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Kiota.Builder.Extensions;

namespace Kiota.Builder
{
    public class OpenApiUrlSpaceNode
    {
        public IDictionary<string, OpenApiUrlSpaceNode> Children { get; set; } = new Dictionary<string, OpenApiUrlSpaceNode>();
        public string Segment {get;set;}
        public string Layer {get;set;}

        public OpenApiPathItem PathItem {get;set;}
        public String Path {get;set;} = "";

        public OpenApiUrlSpaceNode(string segment)
        {
            Segment = segment;
        }
        public static OpenApiUrlSpaceNode Create(OpenApiDocument doc, string layer = "")
        {
            OpenApiUrlSpaceNode root = null;

            var paths = doc?.Paths;
            if (paths != null)
            {
                root = new OpenApiUrlSpaceNode("");

                foreach (var path in paths)
                {
                    root.Attach(path.Key, path.Value, layer);
                }
            }
            return root;
        }

        public void Attach(OpenApiDocument doc, string layer)
        {
            var paths = doc?.Paths;
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    this.Attach(path.Key, path.Value, layer);
                }
            }
        }

        public OpenApiUrlSpaceNode Attach(string path, OpenApiPathItem pathItem, string layer)
        {
            if (path.StartsWith("/"))  // remove leading slash
            {
                path = path.Substring(1);
            }
            var segments = path.Split('/');
            return Attach(segments, pathItem, layer, "");
        }

        private OpenApiUrlSpaceNode Attach(IEnumerable<string> segments, OpenApiPathItem pathItem, string layer, string currentPath)
        {

            var segment = segments.FirstOrDefault();
            if (string.IsNullOrEmpty(segment))
            {
                if (PathItem == null)
                {
                    PathItem = pathItem;
                    Path = currentPath;
                    Layer = layer;
                }
                return this;
            }

            // If the child segment has already been defined, then insert into it
            if (Children.ContainsKey(segment))
            {
                return Children[segment].Attach(segments.Skip(1), pathItem, layer, currentPath + pathNameSeparator + segment );
            }
            else
            {
                var node = new OpenApiUrlSpaceNode(segment);
                node.Path = currentPath + pathNameSeparator + segment;
                Children[segment] = node;
                return node.Attach(segments.Skip(1), pathItem, layer, currentPath + pathNameSeparator + segment);
            }
        }
        private readonly char pathNameSeparator = '\\';
    }
}
