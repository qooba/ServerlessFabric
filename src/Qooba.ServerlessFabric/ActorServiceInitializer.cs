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

        public ActorServiceInitializer(ISerializer serializer)
        {
            this.serializer = serializer;
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
                var methodName = actorMethod.Name;
                var parametersTypes = actorMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                if (parametersTypes.Length > 1)
                {
                    throw new InvalidOperationException("Upps ... TActor method can have only one request parameter");
                }

                var returnType = actorMethod.ReturnType;
                if (returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
                {
                    throw new InvalidOperationException("Upps ... TActor method must be async");
                }

                Type baseReturnType = returnType == typeof(Task) ? null : returnType.GenericTypeArguments.FirstOrDefault();
                var requestName = parametersTypes.Select(x => x.Name).FirstOrDefault();
                var responseName = returnType == typeof(Task) ? string.Empty : baseReturnType.Name;
                var actorMethodKey = ActorMethodHelper.PrepareMethodQueryString(methodName, requestName, responseName);

                actorMethodsCache[actorMethodKey] = PreapreAction(actorInstanceType, actorMethod, parametersTypes.FirstOrDefault(), returnType);
            }
        }

        private Func<TActor, string, Task<object>> PreapreAction(Type actorInstanceType, MethodInfo method, Type requestType, Type responseType)
        {
            var actorType = typeof(TActor);
            var actorParam = Expression.Parameter(actorType);

            Func<TActor, object, Task> callActorMethod = null;
            Func<TActor, Task> callActorNoRequestMethod = null;
            if (requestType != null)
            {
                var requestParam = Expression.Parameter(typeof(object));
                callActorMethod = Expression.Lambda<Func<TActor, object, Task>>(Expression.Convert(Expression.Call(Expression.Convert(actorParam, actorInstanceType), method, Expression.Convert(requestParam, requestType)), typeof(Task)), actorParam, requestParam).Compile();
            }
            else
            {
                callActorNoRequestMethod = Expression.Lambda<Func<TActor, Task>>(Expression.Convert(Expression.Call(Expression.Convert(actorParam, actorInstanceType), method), typeof(Task)), actorParam).Compile();
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
                if (requestType != null)
                {
                    var request = this.serializer.DeserializeObject(req, requestType);
                    resTask = callActorMethod(actor, request);
                }
                else
                {
                    resTask = callActorNoRequestMethod(actor);
                }

                await resTask;
                return getActorMethodResponse != null ? getActorMethodResponse(resTask) : null;
            };

            return func;
        }
    }
}
