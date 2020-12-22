using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace kiota.core
{

    public class RequestBuilder
    {
        internal OpenApiPathItem PathItem
        {
            get
            {
                return pathItem;
            }
            set
            {
                pathItem = value;
            }
        }
        internal string Path = "";
        private OpenApiPathItem pathItem;
        private readonly string segment;

        public RequestBuilder(string segment)
        {
            this.segment = segment;
        }

        public string Hash()
        {
            return Path.GetHashCode().ToString("X");
        }

        public bool IsParameter()
        {
            return segment.StartsWith("{");
        }

        public bool IsFunction()
        {
            return segment.Contains("(");
        }

        public string Identifier
        {
            get
            {
                string identifier;
                if (IsParameter())
                {
                    identifier = this.segment.Substring(1, segment.Length - 2).Replace("-", "");
                    identifier = FirstUpperCase(identifier);
                }
                else
                {
                    identifier = FirstUpperCase(segment).Replace("()", "").Replace("-", "");
                    var openParen = identifier.IndexOf("(");
                    if (openParen >= 0)
                    {
                        identifier = identifier.Substring(0, openParen);
                    }
                }
                return identifier;
            }
        }
        private string FirstUpperCase(string input)
        {
            if (input.Length == 0) return input;
            return Char.ToUpper(input[0]) + input.Substring(1);
        }

        internal bool HasOperations()
        {
            return PathItem != null && PathItem.Operations != null && PathItem.Operations.Count() > 0;
        }

        public TypedQueryBuilder QueryBuilder
        {
            get; set;
        }
        public TypedQueryParameters QueryParameters
        {
            get; set;
        }
        public Dictionary<string, RequestBuilder> Children { get; set; } = new Dictionary<string, RequestBuilder>();
    }
}
