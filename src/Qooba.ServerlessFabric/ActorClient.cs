using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public static class ActorClient
    {
        private static IDictionary<Type, Func<IActorClient>> actorClientFactories = new ConcurrentDictionary<Type, Func<IActorClient>>();

        private static IActorRequestFactory actorRequestFactory;

        private static readonly object alock = new object();

        public static void RegisterActorClient<TActor>(Func<IActorClient> actorClientFactory)
        {
            actorClientFactories[typeof(TActor)] = actorClientFactory;
        }

        public static void SetActorRequestFactory(IActorRequestFactory actorRequestFactory)
        {
            if (ActorClient.actorRequestFactory == null)
            {
                lock (alock)
                {
                    if (ActorClient.actorRequestFactory == null)
                    {
                        ActorClient.actorRequestFactory = actorRequestFactory;
                    }
                }
            }
        }

        public static async Task<TResponse> InvokeRequestResponseMultiple<TActor, TResponse>(string url, string methodName, params object[] request)
        {
            var req = actorRequestFactory.CreateActorResponse(request);
            return await PrepareActorClient<TActor>().Invoke<TActor, TResponse>(url, methodName, req, req.GetType());
        }

        public static async Task InvokeRequestMultiple<TActor>(string url, string methodName, params object[] request)
        {
            var req = actorRequestFactory.CreateActorResponse(request);
            await PrepareActorClient<TActor>().Invoke<TActor>(url, methodName, req, req.GetType());
        }

        public static async Task<TResponse> InvokeRequestResponse<TActor, TRequest, TResponse>(string url, string methodName, TRequest request)
        {
            return await PrepareActorClient<TActor>().Invoke<TActor, TRequest, TResponse>(url, methodName, request);
        }

        public static async Task InvokeRequest<TActor, TRequest>(string url, string methodName, TRequest request)
        {
            await PrepareActorClient<TActor>().Invoke<TActor, TRequest>(url, methodName, request);
        }

        public static async Task<TResponse> InvokeResponse<TActor, TResponse>(string url, string methodName)
        {
            return await PrepareActorClient<TActor>().Invoke<TActor, TResponse>(url, methodName);
        }

        public static async Task Invoke<TActor>(string url, string methodName)
        {
            await PrepareActorClient<TActor>().Invoke<TActor>(url, methodName);
        }

        private static IActorClient PrepareActorClient<TActor>()
        {
            Func<IActorClient> actorFactory;
            if (!actorClientFactories.TryGetValue(typeof(TActor), out actorFactory))
            {
                throw new InvalidOperationException("Upps ... actor client not registered");
            }

            return actorFactory();
        }
    }
}
