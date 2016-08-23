using Newtonsoft.Json;

namespace Qooba.ServerlessFabric
{
    public class ActorRequest<TRequest>
    {
        [JsonProperty(PropertyName = "methodName")]
        public string MethodName { get; set; }

        [JsonProperty(PropertyName = "data")]
        public TRequest Data { get; set; }
    }
}
