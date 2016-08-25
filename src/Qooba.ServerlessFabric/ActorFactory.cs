using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public class ActorFactory : BaseTypeFactory, IActorFactory
    {
        private static readonly Lazy<ActorFactory> instance = new Lazy<ActorFactory>(() => new ActorFactory(new ActorClientManager(), new ActorResponseFactory(), new ExpressionHelper(), new ActorRequestFactory()));

        private static IDictionary<Type, object> actorProxies = new ConcurrentDictionary<Type, object>();

        private readonly IActorClientManager actorClientManager;

        private readonly IActorResponseFactory actorResponseFactory;

        private readonly IExpressionHelper expressionHelper;

        public ActorFactory(IActorClientManager actorClientManager, IActorResponseFactory actorResponseFactory, IExpressionHelper expressionHelper, IActorRequestFactory actorRequestFactory)
        {
            this.actorClientManager = actorClientManager;
            this.actorResponseFactory = actorResponseFactory;
            this.expressionHelper = expressionHelper;
            ActorClient.SetActorRequestFactory(actorRequestFactory);
        }

        public static TActor Create<TActor>(Uri url)
        {
            return Create<TActor>(url, false);
        }

        public static TActor Create<TActor>(Uri url, bool wrapResponse)
        {
            return Create<TActor>(url, () => new ActorHttpClient(new ActorResponseFactory(), new JsonSerializer(), new ExpressionHelper()));
        }

        public static TActor Create<TActor>(Uri url, Func<IActorClient> actorClientFactory)
        {
            return Create<TActor>(url, actorClientFactory, false);
        }

        public static TActor Create<TActor>(Uri url, Func<IActorClient> actorClientFactory, bool wrapResponse)
        {
            return instance.Value.CreateActor<TActor>(url, actorClientFactory, wrapResponse);
        }

        public TActor CreateActor<TActor>(Uri url, Func<IActorClient> actorClientFactory, bool wrapResponse)
        {
            object actor;
            var actorType = typeof(TActor);
            if (!actorProxies.TryGetValue(actorType, out actor))
            {
                actor = PrepareActorProxy<TActor>(url, actorClientFactory, actorType, wrapResponse);
            }

            return (TActor)actor;
        }

        private object PrepareActorProxy<TActor>(Uri url, Func<IActorClient> actorClientFactory, Type actorType, bool wrapResponse)
        {
            object actor;
            if (!actorType.GetTypeInfo().IsInterface)
            {
                throw new InvalidOperationException("Upps ... TActor must be interface");
            }

            var actorMethods = actorType.GetRuntimeMethods();
            var actorProperties = actorType.GetRuntimeProperties();
            if (actorProperties.Count() > 0)
            {
                throw new InvalidOperationException("Upps ... TActor can't have properties");
            }

            TypeBuilder tb = this.ModuleBuilder.DefineType($"{ActorConstants.TYPE_NAME_PREFIX}{actorType.Name}", TypeAttributes.Public | TypeAttributes.Class);
            tb.AddInterfaceImplementation(actorType);
            var actorMethodNames = actorMethods.Select(x => x.Name).ToList();

            foreach (var actorMethod in actorMethods)
            {
                var methodName = actorMethod.Name;
                var parametersTypes = actorMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                var returnType = actorMethod.ReturnType;
                if (returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
                {
                    throw new InvalidOperationException("Upps ... TActor method must be async");
                }

                var meth = tb.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual, returnType, parametersTypes);

                Type baseReturnType = returnType == typeof(Task) ? null : returnType.GenericTypeArguments.FirstOrDefault();
                var invokeMethod = this.actorClientManager.PrepareInvokeMethod<TActor>(actorClientFactory, parametersTypes, baseReturnType);

                ILGenerator methIL = meth.GetILGenerator();
                methIL.Emit(OpCodes.Nop);
                methIL.Emit(OpCodes.Ldstr, url.ToString());
                methIL.Emit(OpCodes.Ldstr, methodName);

                var parametersTypesLength = parametersTypes.Length;
                if (parametersTypesLength == 1)
                {
                    methIL.Emit(OpCodes.Ldarg_1);
                }
                else if (parametersTypesLength > 1)
                {
                    methIL.Emit(OpCodes.Ldc_I4, parametersTypesLength);
                    methIL.Emit(OpCodes.Newarr, typeof(object));
                    for (int i = 0; i < parametersTypesLength; i++)
                    {
                        methIL.Emit(OpCodes.Dup);
                        methIL.Emit(OpCodes.Ldc_I4, i);
                        methIL.Emit(OpCodes.Ldarg_S, i + 1);
                        methIL.Emit(OpCodes.Box, parametersTypes[i]);
                        methIL.Emit(OpCodes.Stelem_Ref);
                    }
                }

                methIL.Emit(OpCodes.Call, invokeMethod);
                methIL.Emit(OpCodes.Ret);

                if (baseReturnType != null)
                {
                    this.actorResponseFactory.PrepareResponseWrapper(baseReturnType, methodName, wrapResponse);
                }
            }

            Type t = tb.CreateTypeInfo().AsType();
            actor = this.expressionHelper.CreateInstance(t);
            actorProxies[actorType] = actor;
            return actor;
        }
    }
}