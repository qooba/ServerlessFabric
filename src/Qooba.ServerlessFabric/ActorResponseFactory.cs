using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;

namespace Qooba.ServerlessFabric
{
    public class ActorResponseFactory : BaseTypeFactory, IActorResponseFactory
    {
        private static IDictionary<Type, Type> actorResponseWrappers = new ConcurrentDictionary<Type, Type>();
        
        public Type CreateActorResponseType<TResponse>()
        {
            return CreateActorResponseType(typeof(TResponse));
        }

        public Type CreateActorResponseType(Type returnType)
        {
            Type wrapperType;
            if (!actorResponseWrappers.TryGetValue(returnType, out wrapperType))
            {
                wrapperType = PrepareResponseWrapper(returnType, null, false);
            }

            return wrapperType;
        }

        public Type PrepareResponseWrapper(Type returnType, string methodName, bool wrapResponse)
        {
            Type wrapperType = returnType;
            var returnTypeInfo = returnType.GetTypeInfo();
            if (wrapResponse && returnTypeInfo.IsClass && returnTypeInfo.GetConstructor(Type.EmptyTypes) != null)
            {
                var name = returnType.Name;
                var responseWrapper = this.ModuleBuilder.DefineType($"{ActorConstants.RESPONSE_TYPE_NAME_PREFIX}{methodName}{name}", TypeAttributes.Public | TypeAttributes.Class);
                responseWrapper.SetParent(returnType);
                responseWrapper.AddInterfaceImplementation(typeof(IActorResponseMessage));

                //STATUS CODE
                CreateProperty(responseWrapper, "StatusCode", typeof(HttpStatusCode));
                CreateProperty(responseWrapper, "Headers", typeof(HttpResponseHeaders));
                CreateProperty(responseWrapper, "ErrorMessage", typeof(string));
                wrapperType = responseWrapper.CreateTypeInfo().AsType();
            }

            actorResponseWrappers[returnType] = wrapperType;
            return wrapperType;
        }
    }
}
