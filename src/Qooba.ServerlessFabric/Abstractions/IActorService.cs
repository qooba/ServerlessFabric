using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorService<TActor>
    {
        Task CreateService(string req, Func<TActor> actorFactory);

        Task<HttpResponseMessage> CreateService(HttpRequestMessage req, Func<TActor> actorFactory);
    }
}
