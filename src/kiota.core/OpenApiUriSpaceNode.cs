using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        public string Hash()
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                return GetHash(sha256Hash,Path);
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < 2; i++)  //data.Length  Limit to 4 chars
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
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
    }

}
