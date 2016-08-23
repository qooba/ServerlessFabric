using System;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface ISerializer
    {
        ActorRequest ParseJsonActorRequest(string json);

        string SerializeObject(object value);

        object DeserializeObject(string value, Type type);

        Task<string> SerializeObjectAsync(object value);

        Task<object> DeserializeObjectAsync(string value, Type type);
    }
}
