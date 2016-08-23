using System;

namespace Qooba.ServerlessFabric.Abstractions
{
    public interface IExpressionHelper
    {
        object CreateInstance(Type type, params object[] arguments);

        T CreateInstance<T>(params object[] arguments) where T : class;
    }
}