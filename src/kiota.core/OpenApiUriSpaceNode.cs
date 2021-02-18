using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class OpenApiUrlSpaceNode
    {
        public IDictionary<string, OpenApiUrlSpaceNode> Children { get; set; } = new Dictionary<string, OpenApiUrlSpaceNode>();
        public string Segment;
        public string Layer;

        public OpenApiPathItem PathItem;
        public String Path = "";

        public OpenApiUrlSpaceNode(string segment)
        {
            Segment = segment;
        }

        public bool IsParameter()
        {
            return Segment.StartsWith("{");
        }

        public bool IsFunction()
        {
            return Segment.Contains("(");
        }

        public string Identifier
        {
            get
            {
                string identifier;
                if (IsParameter())
                {
                    identifier = Segment.Substring(1, Segment.Length - 2).ToPascalCase();
                }
                else
                {
                    identifier = Segment.ToPascalCase().Replace("()", "");
                    var openParen = identifier.IndexOf("(");
                    if (openParen >= 0)
                    {
                        identifier = identifier.Substring(0, openParen);
                    }
                }
                return identifier;
            }
        }

        internal bool HasOperations()
        {
            return PathItem != null && PathItem.Operations != null && PathItem.Operations.Count() > 0;
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
                return Children[segment].Attach(segments.Skip(1), pathItem, layer, currentPath + "\\" + segment );
            }
            else
            {
                var node = new OpenApiUrlSpaceNode(segment);
                node.Path = currentPath + "\\" + segment;
                Children[segment] = node;
                return node.Attach(segments.Skip(1), pathItem, layer, currentPath + "\\" + segment);
            }
        }
        public bool DoesNodeBelongToItemSubnamespace() =>
        (Segment?.StartsWith("{") ?? false) && (Segment?.EndsWith("}") ?? false);
        private readonly char pathNameSeparator = '\\';
        public string GetNodeNamespaceFromPath(string prefix = default) =>
            prefix + 
                    ((Path?.Contains(pathNameSeparator) ?? false) ?
                        "." + Path
                                ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                ?.Where(x => !x.StartsWith('{'))
                                ?.Aggregate((x, y) => $"{x}.{y}") :
                        string.Empty)
                    .ReplaceValueIdentifier();
        
        public string GetClassName(string suffix = default, string prefix = default) {
            var rawClassName = Identifier?.ReplaceValueIdentifier();
            if(DoesNodeBelongToItemSubnamespace() && rawClassName.EndsWith("Id"))
                rawClassName = rawClassName.Substring(0, rawClassName.Length - 2);
            return prefix + rawClassName + suffix;
        }
    }
}
