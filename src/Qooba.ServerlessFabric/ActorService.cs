using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using Qooba.ServerlessFabric.Abstractions;

namespace Qooba.ServerlessFabric
{
    public class ActorService<TActor> : IActorService<TActor>
    {
        private static readonly Lazy<ActorService<TActor>> instance = new Lazy<ActorService<TActor>>(() => new ActorService<TActor>(new ActorServiceInitializer<TActor>(new JsonSerializer(), new ActorRequestFactory()), new JsonSerializer()));

        private readonly IActorServiceInitializer<TActor> actorServiceInitializer;

        private readonly ISerializer serializer;

        public ActorService(IActorServiceInitializer<TActor> actorServiceInitializer, ISerializer serializer)
        {
            this.actorServiceInitializer = actorServiceInitializer;
            this.serializer = serializer;
        }

        public static async Task Create(string req, Func<TActor> actorFactory)
        {
            await instance.Value.CreateService(req, actorFactory);
        }

        public static async Task<HttpResponseMessage> Create(HttpRequestMessage req, Func<TActor> actorFactory)
        {
            return await instance.Value.CreateService(req, actorFactory);
        }

        public async Task CreateService(string req, Func<TActor> actorFactory)
        {
            var actorInstance = actorFactory();
            var actorRequest = this.serializer.ParseJsonActorRequest(req);
            var actorMethod = this.actorServiceInitializer.PreapareActorMethod(actorInstance, actorRequest.MethodName);
            await actorMethod(actorInstance, actorRequest.Data);
        }

        public async Task<HttpResponseMessage> CreateService(HttpRequestMessage req, Func<TActor> actorFactory)
        {
            var actorInstance = actorFactory();

#if (NET46 || NET461)
            string methodName = req.RequestUri.ParseQueryString()[ActorConstants.METHOD_NAME];
#else
            string methodName = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(req.RequestUri.Query)[ActorConstants.METHOD_NAME];
#endif

            var actorMethod = this.actorServiceInitializer.PreapareActorMethod(actorInstance, methodName);
            var request = await req.Content.ReadAsStringAsync();
            var response = await actorMethod(actorInstance, request);
            if (response != null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(this.serializer.SerializeObject(response), Encoding.UTF8, "application/json")
                };
            }
            else
            {

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}