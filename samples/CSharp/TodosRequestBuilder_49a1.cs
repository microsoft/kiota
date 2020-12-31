using System;
using System.Threading.Tasks;
namespace Todo {
    public class TodosRequestBuilder_49a1 {
        public TodoIdRequestBuilder_36d6 this[string TodoId] {get { return null; } }
        public Task<object> GetAsync(Action<GetQueryParameters> q = default(Action<GetQueryParameters>)) { return null; }
        public Task<object> PostAsync(Action<PostQueryParameters> q = default(Action<PostQueryParameters>)) { return null; }
        public class GetQueryParameters {
            public bool active {get;}
            public string keyword {get;}
        }
        public class PostQueryParameters {
        }
    }
}
