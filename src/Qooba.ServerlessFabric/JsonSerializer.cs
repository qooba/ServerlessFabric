using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public class JsonSerializer : ISerializer
    {
        public ActorRequest ParseJsonActorRequest(string json)
        {
            var jObject = JObject.Parse(json);
            return new ActorRequest
            {
                Data = (string)jObject["data"],
                MethodName = (string)jObject["methodName"]
            };
        }

        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public async Task<object> DeserializeObjectAsync(string value, Type type)
        {
            return await Task.Run(() => this.DeserializeObject(value, type));
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public async Task<string> SerializeObjectAsync(object value)
        {
            return await Task.Run(() => this.SerializeObject(value));
        }
    }
}
