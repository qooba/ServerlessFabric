using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Qooba.ServerlessFabric.Tests
{
    public class ActorResponseFactoryTests
    {
        private readonly IActorResponseFactory actorResponseFactory;

        public ActorResponseFactoryTests()
        {
            this.actorResponseFactory = new ActorResponseFactory();
        }

        [Fact]
        public void CreateActorResponseTypeTest()
        {
            var responseType = this.actorResponseFactory.CreateActorResponseType<TestResponseType>();
            Assert.True(responseType.GetInterfaces().Contains(typeof(IActorResponseMessage)));
        }

        [Fact]
        public void PrepareResponseWrapper()
        {
            var responseType = this.actorResponseFactory.PrepareResponseWrapper(typeof(TestResponseType), "MyMethod", true);
            Assert.True(responseType.GetInterfaces().Contains(typeof(IActorResponseMessage)));
        }

        public class TestResponseType
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
