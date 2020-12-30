# Request Builder

The primary goal of a request builder class is to enable a developer to easily construct an HTTP request. By using a set of related request builder classes we can enable a developer to contruct a HTTP request for any resource exposed by an HTTP API.

There are three phases of constructing the HTTP request. Building the path, specifying query parameters and selecting the HTTP method.

## Building the Path


By creating properties on request builder classes, the developer can effectively construct a URL path by navigating the hierarchy of request builders, aided by the IDE's autocomplete.

```csharp

namespace Todo {
    public class TodoClient : BaseRequestBuilder  {
        
        public TodoClient(HttpClient httpClient, string basePath) : base(httpClient, path) {

        }

        public TodosRequestBuilder Todos {
            get { 
                return new TodosRequestBuilder(this.httpClient, Path + "todos");
                }
            }
    }

    public class TodosRequestBuilder {
        public TodoIdRequestBuilder this[string TodoId] {
            get { 
                return new TodoIdRequestBuilder(this.HttpClient, Path + "/" + TodoId);
                }
            }
    }

    public class TodoIdRequestBuilder {

    }
}

```
Each request builder class exposes the set of HTTP methods that are supported on that resource.

```csharp

namespace Todo {
    public class TodoClient : BaseRequestBuilder  {
        
        public TodoClient(HttpClient httpClient, string basePath) : base(httpClient, path) {

        }

        public TodosRequestBuilder Todos {
            get { 
                return new TodosRequestBuilder(this.httpClient, Path + "todos");
                }
            }
    }

    public class TodosRequestBuilder {
        public TodoIdRequestBuilder this[string TodoId] {
            get { 
                return new TodoIdRequestBuilder(this.HttpClient, Path + "/" + TodoId);
                }
            }
        public Task<object> GetAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>)) 
        { 
            return ResponseHandler(base.SendAsync(this.ToHttpRequestMessage(HttpMethod.Get))); 
        }
        public Task<object> PostAsync(object todo) 
        { 
            return ResponseHandler(base.SendAsync(this.ToHttpRequestMessage(HttpMethod.Post, this.CreateContent(todo)))); 
        }

        public class GetQueryParameters {
            public bool active {get;}
            public string keyword {get;}
        }
    }

    public class TodoIdRequestBuilder {

        public Task<object> GetAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>)) {         
            return ResponseHandler(base.SendAsync(this.ToHttpRequestMessage(HttpMethod.Get))); 
        }
        public Task<object> DeleteAsync(Action<DeleteQueryParameters> q = default(Action<DeleteQueryParameters>)) 
        { 
           return ResponseHandler(base.SendAsync(this.ToHttpRequestMessage(HttpMethod.Delete))); 
        }

        public class GetQueryParameters {
        }
        public class DeleteQueryParameters {
        }
    }
}

```

Once the desired resource is selected, the developer can then select the required HTTP method and execute the call.

```csharp
    var client = new TodoClient(new HttpClient());
    var todo = client.Todos[123].GetAsync();
```