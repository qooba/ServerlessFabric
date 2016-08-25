using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public class ActorHttpClient : IActorClient
    {
        private readonly IActorResponseFactory actorResponseFactory;

        private readonly ISerializer serializer;

        private readonly IExpressionHelper expressionHelper;

        public ActorHttpClient(IActorResponseFactory actorResponseFactory, ISerializer serializer, IExpressionHelper expressionHelper)
        {
            this.actorResponseFactory = actorResponseFactory;
            this.serializer = serializer;
            this.expressionHelper = expressionHelper;
        }

        public async Task<TResponse> Invoke<TActor, TRequest, TResponse>(string url, string methodName, TRequest request)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, typeof(TRequest).Name, typeof(TResponse).Name);
                HttpResponseMessage response = await PostAsync(request, client);
                return await PrepareResponse<TResponse>(response);
            }
        }

        public async Task<TResponse> Invoke<TActor, TResponse>(string url, string methodName, object request, Type requestType)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, requestType.Name, typeof(TResponse).Name);
                HttpResponseMessage response = await PostAsync(request, client);
                return await PrepareResponse<TResponse>(response);
            }
        }

        public async Task Invoke<TActor, TRequest>(string url, string methodName, TRequest request)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, typeof(TRequest).Name, string.Empty);
                var response = await PostAsync(request, client);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception(errorMessage);
                }
            }
        }

        public async Task Invoke<TActor>(string url, string methodName, object request, Type requestType)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, requestType.Name, string.Empty);
                var response = await PostAsync(request, client);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception(errorMessage);
                }
            }
        }

        public async Task<TResponse> Invoke<TActor, TResponse>(string url, string methodName)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, string.Empty, typeof(TResponse).Name);
                var response = await client.GetAsync(string.Empty);
                return await PrepareResponse<TResponse>(response);
            }
        }

        public async Task Invoke<TActor>(string url, string methodName)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = PrepareUri(url, methodName, string.Empty, string.Empty);
                var response = await client.GetAsync(string.Empty);
            }
        }

        private async Task<TResponse> PrepareResponse<TResponse>(HttpResponseMessage response)
        {
            var actorResponseWrapper = this.actorResponseFactory.CreateActorResponseType<TResponse>();
            TResponse resp;
            IActorResponseMessage actorResponseMessage;
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                resp = (TResponse)this.serializer.DeserializeObject(jsonContent, actorResponseWrapper);
                actorResponseMessage = resp as IActorResponseMessage;
            }
            else
            {
                resp = (TResponse)this.expressionHelper.CreateInstance(actorResponseWrapper);
                var errorMessage = await response.Content.ReadAsStringAsync();
                actorResponseMessage = resp as IActorResponseMessage;
                if (actorResponseMessage != null)
                {
                    actorResponseMessage.ErrorMessage = errorMessage;
                }
                else
                {
                    throw new Exception(errorMessage);
                }
            }

            if (actorResponseMessage != null)
            {
                actorResponseMessage.StatusCode = response.StatusCode;
                actorResponseMessage.Headers = response.Headers;
            }

            return resp;
        }

        private Uri PrepareUri(string url, string methodName, string requestName, string responseName)
        {
            var requestUri = new Uri(url);

            var methodNameQueryString = ActorMethodHelper.PrepareMethodQueryString(methodName, requestName, responseName);

#if (NET46 || NET461)
            if (requestUri.ParseQueryString().Count > 0)
#else
            if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(requestUri.Query).Any())
#endif
            {
                url += $"&{ActorConstants.METHOD_NAME}={methodNameQueryString}";
            }
            else
            {
                url = $"?{ActorConstants.METHOD_NAME}={methodNameQueryString}";
            }

            return new Uri(url);
        }

        private async Task<HttpResponseMessage> PostAsync<TRequest>(TRequest request, HttpClient client)
        {
            return await client.PostAsync(string.Empty, new StringContent(this.serializer.SerializeObject(request), Encoding.UTF8, "application/json"));
        }
    }
}
