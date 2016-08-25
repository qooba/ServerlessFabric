using Newtonsoft.Json;
using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public class ActorServiceInitializer<TActor> : IActorServiceInitializer<TActor>
    {
        private static IDictionary<string, Func<TActor, string, Task<object>>> actorMethodsCache = new ConcurrentDictionary<string, Func<TActor, string, Task<object>>>();

        private readonly ISerializer serializer;

        private readonly IActorRequestFactory actorRequestFactory;

        public ActorServiceInitializer(ISerializer serializer, IActorRequestFactory actorRequestFactory)
        {
            this.serializer = serializer;
            this.actorRequestFactory = actorRequestFactory;
        }

        public Func<TActor, string, Task<object>> PreapareActorMethod(TActor actorInstance, string methodName)
        {
            if (actorMethodsCache.Count == 0)
            {
                InitializeActorType(actorInstance);
            }

            return actorMethodsCache[methodName];
        }

        private void InitializeActorType(TActor actorInstance)
        {
            var actorType = typeof(TActor);
            var actorInstanceType = actorInstance.GetType();
            var actorMethods = actorType.GetRuntimeMethods();
            var actorProperties = actorType.GetRuntimeProperties();
            if (actorProperties.Count() > 0)
            {
                throw new InvalidOperationException("Upps ... TActor can't have properties");
            }

            foreach (var actorMethod in actorMethods)
            {

                var parametersTypes = actorMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                var returnType = actorMethod.ReturnType;
                if (returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
                {
                    throw new InvalidOperationException("Upps ... TActor method must be async");
                }

                PreapreAction(actorInstanceType, actorMethod, parametersTypes, returnType);
            }
        }

        private void PreapreAction(Type actorInstanceType, MethodInfo method, Type[] parametersTypes, Type responseType)
        {
            var actorType = typeof(TActor);
            var actorParam = Expression.Parameter(actorType);
            var parametersTypesNumber = parametersTypes.Count();
            Func<TActor, object, Task> callActorMethod = null;
            Func<TActor, Task> callActorNoRequestMethod = null;
            Type requestType = null;

            if (parametersTypesNumber == 0)
            {
                callActorNoRequestMethod = Expression.Lambda<Func<TActor, Task>>(Expression.Convert(Expression.Call(Expression.Convert(actorParam, actorInstanceType), method), typeof(Task)), actorParam).Compile();
            }
            else if (parametersTypesNumber == 1)
            {
                requestType = parametersTypes.FirstOrDefault();
                var requestParam = Expression.Parameter(typeof(object));
                callActorMethod = Expression.Lambda<Func<TActor, object, Task>>(Expression.Convert(Expression.Call(Expression.Convert(actorParam, actorInstanceType), method, Expression.Convert(requestParam, requestType)), typeof(Task)), actorParam, requestParam).Compile();
            }
            else
            {
                requestType = this.actorRequestFactory.CreateActorRequestType(parametersTypes);
                var requestParam = Expression.Parameter(typeof(object));
                var expressionAgruments = new List<Expression>();
                for (int i = 0; i < parametersTypes.Count(); i++)
                {
                    var property = Expression.Property(Expression.Convert(requestParam, requestType), $"Prop{i}");
                    expressionAgruments.Add(property);
                }

                callActorMethod = Expression.Lambda<Func<TActor, object, Task>>(Expression.Convert(Expression.Call(Expression.Convert(actorParam, actorInstanceType), method, expressionAgruments), typeof(Task)), actorParam, requestParam).Compile();
            }

            Func<Task, object> getActorMethodResponse = null;
            if (responseType != typeof(Task))
            {
                var responseParameter = Expression.Parameter(typeof(Task));
                var resultProperty = responseType.GetRuntimeProperty("Result");
                getActorMethodResponse = Expression.Lambda<Func<Task, object>>(Expression.Property(Expression.Convert(responseParameter, responseType), resultProperty), responseParameter).Compile();
            }

            Func<TActor, string, Task<object>> func = async (actor, req) =>
            {
                Task resTask = null;

                if (requestType == null)
                {
                    resTask = callActorNoRequestMethod(actor);
                }
                else
                {
                    var request = this.serializer.DeserializeObject(req, requestType);
                    resTask = callActorMethod(actor, request);
                }

                await resTask;
                return getActorMethodResponse != null ? getActorMethodResponse(resTask) : null;
            };


            var methodName = method.Name;
            var requestName = requestType != null ? requestType.Name : string.Empty;
            var baseReturnType = responseType == typeof(Task) ? null : responseType.GenericTypeArguments.FirstOrDefault();
            var responseName = responseType == typeof(Task) ? string.Empty : baseReturnType.Name;
            var actorMethodKey = ActorMethodHelper.PrepareMethodQueryString(methodName, requestName, responseName);
            actorMethodsCache[actorMethodKey] = func;
        }
    }
}
