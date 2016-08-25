using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Qooba.ServerlessFabric
{
    public class ActorRequestFactory : BaseTypeFactory, IActorRequestFactory
    {
        private static IDictionary<string, Type> actorRequestWrappers = new ConcurrentDictionary<string, Type>();

        private static IDictionary<Type, Func<object[], object>> actorRequestAssigners = new ConcurrentDictionary<Type, Func<object[], object>>();
        
        public object CreateActorResponse(object[] parameters)
        {
            var requestType = CreateActorRequestType(parameters);
            var assigner = PrepareRequestAssigner(requestType, parameters);
            return assigner(parameters);
        }

        public IEnumerable<Expression> CreateActorResponseReader<TActor>(IList<Type> parametersTypes)
        {
            var requestType = CreateActorRequestType(parametersTypes);
            var requestParameter = Expression.Parameter(requestType, "request");
            var expressionAgruments = new List<Expression>();
            for (int i = 0; i < parametersTypes.Count; i++)
            {
                var property = Expression.Property(requestParameter, $"Prop{i}");
                expressionAgruments.Add(property);
            }

            return expressionAgruments;
        }

        public Type CreateActorRequestType(IEnumerable<object> parameters)
        {
            var parametersTypes = parameters.Select(p => p.GetType()).ToList();
            return CreateActorRequestType(parametersTypes);
        }

        public Type CreateActorRequestType(IList<Type> parametersTypes)
        {
            var typeName = string.Join("_", parametersTypes.Select(p => p.FullName.Replace(".", "_")));
            Type wrapperType;
            if (!actorRequestWrappers.TryGetValue(typeName, out wrapperType))
            {
                wrapperType = PrepareRequestWrapper(typeName, parametersTypes);
            }

            return wrapperType;
        }

        private Func<object[], object> PrepareRequestAssigner(Type requestType, object[] parameters)
        {
            Func<object[], object> assigner;
            if (!actorRequestAssigners.TryGetValue(requestType, out assigner))
            {
                var ctor = requestType.GetConstructors().FirstOrDefault();
                var outputInstance = Expression.New(ctor);
                var outputLocal = Expression.Parameter(typeof(object), "outputLocal");
                var assignExpressions = new List<Expression>();
                var localVariables = new List<ParameterExpression>();
                localVariables.Add(outputLocal);
                assignExpressions.Add(Expression.Assign(outputLocal, outputInstance));

                var parametersExpression = Expression.Parameter(typeof(object[]), "parameters");
                var requestProperties = requestType.GetProperties();

                for (int i = 0; i < parameters.Length; i++)
                {
                    var property = requestProperties.FirstOrDefault(x => x.Name == $"Prop{i}");
                    var value = Expression.Convert(Expression.ArrayIndex(parametersExpression, Expression.Constant(i)), property.PropertyType);
                    var prop = Expression.Property(Expression.Convert(outputLocal, requestType), property.Name);
                    var assign = Expression.Assign(prop, value);
                    assignExpressions.Add(assign);
                }

                assignExpressions.Add(outputLocal);
                var assignBlock = Expression.Block(localVariables, assignExpressions);
                assigner = Expression.Lambda<Func<object[], object>>(assignBlock, parametersExpression).Compile();
                actorRequestAssigners[requestType] = assigner;
            }

            return assigner;
        }

        private Type PrepareRequestWrapper(string typeName, IList<Type> parametersTypes)
        {
            var requestWrapper = this.ModuleBuilder.DefineType($"{ActorConstants.REQUEST_TYPE_NAME_PREFIX}{typeName}", TypeAttributes.Public | TypeAttributes.Class);
            for (int i = 0; i < parametersTypes.Count; i++)
            {
                var parameter = parametersTypes[i];
                CreateProperty(requestWrapper, $"Prop{i}", parameter);
            }


            var wrapperType = requestWrapper.CreateTypeInfo().AsType(); ;
            actorRequestWrappers[typeName] = wrapperType;
            return wrapperType;
        }
    }
}
