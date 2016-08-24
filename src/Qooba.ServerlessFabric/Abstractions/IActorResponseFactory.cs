using System;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorResponseFactory
    {
        Type CreateActorResponseType<TResponse>();

        Type CreateActorResponseType(Type returnType);

        Type PrepareResponseWrapper(Type returnType, string methodName, bool wrapResponse);
    }
}
