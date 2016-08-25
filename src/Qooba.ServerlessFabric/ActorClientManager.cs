using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Qooba.ServerlessFabric
{
    public class ActorClientManager : IActorClientManager
    {
        public MethodInfo PrepareInvokeMethod<TActor>(Func<IActorClient> actorClientFactory, IEnumerable<Type> parametersTypes, Type returnType)
        {
            var actorType = typeof(TActor);
            ActorClient.RegisterActorClient<TActor>(actorClientFactory);
            var actorClientMethods = typeof(ActorClient).GetRuntimeMethods();
            var parametersTypesNumber = parametersTypes.Count();
            if (parametersTypesNumber == 0)
            {
                if (returnType == null)
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_VOID).MakeGenericMethod(actorType);
                }
                else
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_RESPONSE).MakeGenericMethod(actorType, returnType);
                }
            }
            else if (parametersTypesNumber == 1)
            {
                var parameterType = parametersTypes.FirstOrDefault();
                if (returnType == null)
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_REQUEST).MakeGenericMethod(actorType, parameterType);
                }
                else
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_REQUEST_RESPONSE).MakeGenericMethod(actorType, parameterType, returnType);
                }
            }
            else
            {
                var parameterType = parametersTypes.FirstOrDefault();
                if (returnType == null)
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_REQUEST_MULTIPLE).MakeGenericMethod(actorType);
                }
                else
                {
                    return actorClientMethods.FirstOrDefault(x => x.Name == ActorConstants.CLIENT_REQUEST_RESPONSE_MULTIPLE).MakeGenericMethod(actorType, returnType);
                }
            }
        }
    }
}
