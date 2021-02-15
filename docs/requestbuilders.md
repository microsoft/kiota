# Request Builder

The primary goal of a request builder class is to enable a developer to easily construct an HTTP request. By using a set of related request builder classes we can enable a developer to contruct a HTTP request for any resource exposed by an HTTP API.

There are three phases of constructing the HTTP request. Building the path, specifying query parameters and selecting the HTTP method.

## Building the Path


By creating properties on request builder classes, the developer can effectively construct a URL path by navigating the hierarchy of request builders, aided by the IDE's autocomplete.

```csharp

namespace Todo {

    public class RequestInfo {
        string Path;
        Dictionary<string,object> QueryParameters = new Dictionary<string,string>();
        Dictionary<string,string> Headers = new Dictionary<string,string>();
    }

    public interface IHttpCore<NativeResponse> {
        Task<Stream> SendAsync(RequestInfo requestInfo);
        Task<NativeResponse> SendNativeAsync(RequestInfo requestInfo);
    }

    public class TodoClient   {
        private RequestInfo requestInfo = new RequestInfo();
        private IHttpCode httpCore;

        public TodoClient(IHttpCore httpCore, string basePath)  {
            requestInfo.AddPath(path);
        }

        public TodosRequestBuilder Todos {

            get { 
                return new TodosRequestBuilder(this.httpCore, requestInfo.Path + "todos");
                }
            }
    }

    public class TodosRequestBuilder {
        private RequestInfo requestInfo = new RequestInfo();
        private IHttpCode httpCore;

        public TodosRequestBuilder(IHttpCore httpCore, string basePath)  {
            requestInfo.AddPath(path);
        }

        public TodoIdRequestBuilder this[string TodoId] {
            get { 
                return new TodoIdRequestBuilder(this.httpCore, Path + "/" + TodoId);
                }
            }
    }

    public class TodoIdRequestBuilder {
        private RequestInfo requestInfo = new RequestInfo();
        private IHttpCode httpCore;

    }
}

```
Each request builder class exposes the set of HTTP methods that are supported on that resource.

```csharp

namespace Todo {

    var client = new TodoClient();

    public class TodoClient: TodoClient<GraphResponse> {

    }

    public class TodoClient<T>
    {
        private RequestInfo requestInfo = new RequestInfo();
        private IHttpCore httpCore;

        public TodoClient(IHttpCore httpCore, string path)
        {
            requestInfo.AddPath(path);
            this.httpCore = httpCore;
        }

        
        public TodosRequestBuilder Todos
        {
            get
            {
                return new TodosRequestBuilder(this.httpCore, requestInfo.Path + "todos");
            }
        }
    }

    public class TodosRequestBuilder<NativeResponse>
    {
        private RequestInfo requestInfo = new RequestInfo();
        private IHttpCore httpCore;

        public TodosRequestBuilder(IHttpCore httpCore, string path)
        {
            this.httpCore = httpCore;
            this.requestInfo.AddPath(path);
        }
        public static Task<IEnumerable<Todo>> DefaultResponseHandlerAsync(Stream content) { return null; }
        public Func<Stream, Task<IEnumerable<Todo>>> ResponseHandler { get; set; } = DefaultResponseHandlerAsync;
        public Func<NativeResponse, Task<IEnumerable<Todo>>> NativeResponseHandler { get; set; } = DefaultResponseHandlerAsync;
        public Func<Todo, Stream> CreateContent { get; set; }

        public TodoIdRequestBuilder this[string TodoId]
        {
            get
            {
                return new TodoIdRequestBuilder(this.httpCore, requestInfo.Path + "/" + TodoId);
            }
        }

        public async Task<NativeResponse> GetNativeResponseAsycn() {
            
        }

        public async Task<IEnumerable<Todo>> GetAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>))
        {
            return await ResponseHandler(await this.httpCore.SendAsync(this.requestInfo.With(RequestMethod.get).WithParameters(q)));
        }

        public async Task<NativeResponse> GetNativeAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>))
        {
            return await this.httpCore.SendNativeAsync(this.requestInfo.With(RequestMethod.get).WithParameters(q));
        }

        public async Task<IEnumerable<Todo>> PostAsync(Todo todo)
        {
            RequestInfo request = this.requestInfo.With(RequestMethod.post).WithContent(this.CreateContent(todo));
            return await ResponseHandler(await this.httpCore.SendAsync(request));
        }

        public class GetQueryParameters : QueryParameters
        {
            public bool active
            {
                get { return Get<bool>("active"); }
                set { Set("active", value); }
            }
            public string keyword
            {
                get { return Get<string>("keyword"); }
                set { Set("keyword", value); }
            }
        }
    }

    public class TodoIdRequestBuilder
    {
        private readonly RequestInfo requestInfo = new RequestInfo();
        private readonly IHttpCore httpCore;

        public TodoIdRequestBuilder(IHttpCore httpCore, string path)
        {
            this.httpCore = httpCore;
            this.requestInfo.AddPath(path);
        }

        public static Todo DefaultResponseHandlerAsync(Stream content) { return null; }
        public Func<Stream, Todo> ResponseHandler { get; set; } = DefaultResponseHandlerAsync;

        public async Task<Todo> GetAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>))
        {
            return ResponseHandler(await this.httpCore.SendAsync(this.requestInfo.With(RequestMethod.get)));
        }
        public async Task<Todo> DeleteAsync(Action<DeleteQueryParameters> q = default(Action<DeleteQueryParameters>))
        {
            return ResponseHandler(await this.httpCore.SendAsync(this.requestInfo.With(RequestMethod.delete)));
        }

        public class GetQueryParameters
        {
        }
        public class DeleteQueryParameters
        {
        }
    }
}

```

Once the desired resource is selected, the developer can then select the required HTTP method and execute the call.

```csharp
    var client = new TodoClient(new HttpClient());
    var todo = client.Todos[123].GetAsync();
```