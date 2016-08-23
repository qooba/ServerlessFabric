using System;
using System.Collections.Generic;
using System.Reflection;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorClientManager
    {
        MethodInfo PrepareInvokeMethod<TActor>(Func<IActorClient> actorClientFactory, IEnumerable<Type> parametersTypes, Type returnType);
    }
}
