using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;

namespace Qooba.ServerlessFabric
{
    public class ActorResponseFactory : IActorResponseFactory
    {
        private static IDictionary<Type, Type> actorResponseWrappers = new ConcurrentDictionary<Type, Type>();

#if (NET46 || NET461)
        private static Lazy<ModuleBuilder> mb = new Lazy<ModuleBuilder>(() => AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ActorConstants.RESPONSE_ASSEMBLY_NAME), AssemblyBuilderAccess.Run).DefineDynamicModule(ActorConstants.RESPONSE_MODULE_NAME));
#else
        private static Lazy<ModuleBuilder> mb = new Lazy<ModuleBuilder>(() => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ActorConstants.RESPONSE_ASSEMBLY_NAME), AssemblyBuilderAccess.Run).DefineDynamicModule(ActorConstants.RESPONSE_MODULE_NAME));
#endif

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
                var responseWrapper = mb.Value.DefineType($"{ActorConstants.RESPONSE_TYPE_NAME_PREFIX}{methodName}{name}", TypeAttributes.Public | TypeAttributes.Class);
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

        private static void CreateProperty(TypeBuilder responseWrapper, string propertyName, Type propertyType)
        {
            FieldBuilder priv = responseWrapper.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);
            PropertyBuilder prop = responseWrapper.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodAttributes propAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            MethodBuilder propGetAccessor = responseWrapper.DefineMethod($"get_{propertyName}", propAttributes, propertyType, Type.EmptyTypes);

            ILGenerator propGetIL = propGetAccessor.GetILGenerator();
            propGetIL.Emit(OpCodes.Ldarg_0);
            propGetIL.Emit(OpCodes.Ldfld, priv);
            propGetIL.Emit(OpCodes.Ret);

            MethodBuilder propSetAccessor = responseWrapper.DefineMethod($"set_{propertyName}", propAttributes, null, new Type[] { propertyType });

            ILGenerator propSetIL = propSetAccessor.GetILGenerator();
            propSetIL.Emit(OpCodes.Ldarg_0);
            propSetIL.Emit(OpCodes.Ldarg_1);
            propSetIL.Emit(OpCodes.Stfld, priv);
            propSetIL.Emit(OpCodes.Ret);

            prop.SetGetMethod(propGetAccessor);
            prop.SetSetMethod(propSetAccessor);
        }
    }
}
