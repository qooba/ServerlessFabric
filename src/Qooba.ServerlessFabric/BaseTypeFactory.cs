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
    public abstract class BaseTypeFactory
    {
        private static IDictionary<Type, Type> actorResponseWrappers = new ConcurrentDictionary<Type, Type>();

#if (NET46 || NET461)
        private static Lazy<ModuleBuilder> mb = new Lazy<ModuleBuilder>(() => AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ActorConstants.ASSEMBLY_NAME), AssemblyBuilderAccess.Run).DefineDynamicModule(ActorConstants.MODULE_NAME));
#else
        private static Lazy<ModuleBuilder> mb = new Lazy<ModuleBuilder>(() => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ActorConstants.ASSEMBLY_NAME), AssemblyBuilderAccess.Run).DefineDynamicModule(ActorConstants.MODULE_NAME));
#endif

        protected ModuleBuilder ModuleBuilder
        {
            get
            {
                return mb.Value;
            }
        }

        protected void CreateProperty(TypeBuilder responseWrapper, string propertyName, Type propertyType)
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
