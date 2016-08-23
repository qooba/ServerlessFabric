using System;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorServiceInitializer<TActor>
    {
        Func<TActor, string, Task<object>> PreapareActorMethod(TActor actorInstance, string methodName);
    }
}
