using System;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorFactory
    {
        TActor CreateActor<TActor>(Uri url, Func<IActorClient> actorClientFactory);
    }
}