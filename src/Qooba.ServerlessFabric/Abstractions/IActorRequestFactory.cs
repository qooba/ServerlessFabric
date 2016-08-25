using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IActorRequestFactory
    {
        Type CreateActorRequestType(IList<Type> parametersTypes);

        Type CreateActorRequestType(IEnumerable<object> parameters);

        object CreateActorResponse(object[] parameters);
    }
}
