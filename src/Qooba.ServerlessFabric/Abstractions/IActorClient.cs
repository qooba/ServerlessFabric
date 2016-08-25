using System;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorClient
    {
        Task<TResponse> Invoke<TActor, TRequest, TResponse>(string url, string methodName, TRequest request);

        Task<TResponse> Invoke<TActor, TResponse>(string url, string methodName, object request, Type requestType);

        Task Invoke<TActor, TRequest>(string url, string methodName, TRequest request);

        Task Invoke<TActor>(string url, string methodName, object request, Type requestType);

        Task<TResponse> Invoke<TActor, TResponse>(string url, string methodName);

        Task Invoke<TActor>(string url, string methodName);
    }
}
