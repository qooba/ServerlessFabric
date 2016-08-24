using Moq;
using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Qooba.ServerlessFabric.Tests
{
    public class ActorFactoryTests
    {
        private readonly IActorFactory actorFactory;

        private readonly Mock<IActorClientManager> actorClientManagerMock;

        private readonly Mock<IActorClient> actorClientMock;

        private readonly Mock<IActorResponseFactory> actorResponseFactoryMock;

        private readonly Mock<IExpressionHelper> expressionHelperMock;

        public ActorFactoryTests()
        {
            this.actorClientMock = new Mock<IActorClient>();
            this.actorClientManagerMock = new Mock<IActorClientManager>();
            this.actorClientManagerMock.Setup(x => x.PrepareInvokeMethod<ITestActor>(It.IsAny<Func<IActorClient>>(), It.IsAny<IEnumerable<Type>>(), It.IsAny<Type>())).Returns(typeof(ActorFactoryTests).GetRuntimeMethods().FirstOrDefault(x => x.Name == "TestMethod"));
            this.actorClientManagerMock.Setup(x => x.PrepareInvokeMethod<ITestActorStr>(It.IsAny<Func<IActorClient>>(), It.IsAny<IEnumerable<Type>>(), It.IsAny<Type>())).Returns(typeof(ActorFactoryTests).GetRuntimeMethods().FirstOrDefault(x => x.Name == "TestMethod"));
            this.actorResponseFactoryMock = new Mock<IActorResponseFactory>();
            this.expressionHelperMock = new Mock<IExpressionHelper>();
            this.actorFactory = new ActorFactory(this.actorClientManagerMock.Object, this.actorResponseFactoryMock.Object, this.expressionHelperMock.Object);
        }

        [Fact]
        public void CreateActorTest()
        {
            this.actorFactory.CreateActor<ITestActor>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true);
            this.actorClientManagerMock.Verify(x => x.PrepareInvokeMethod<ITestActor>(It.IsAny<Func<IActorClient>>(), It.IsAny<IEnumerable<Type>>(), It.IsAny<Type>()));
            this.expressionHelperMock.Verify(x => x.CreateInstance(It.IsAny<Type>()));
        }

        [Fact]
        public void CreateActorStrTest()
        {
            this.actorFactory.CreateActor<ITestActorStr>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true);
            this.actorClientManagerMock.Verify(x => x.PrepareInvokeMethod<ITestActorStr>(It.IsAny<Func<IActorClient>>(), It.IsAny<IEnumerable<Type>>(), It.IsAny<Type>()));
            this.expressionHelperMock.Verify(x => x.CreateInstance(It.IsAny<Type>()));
        }

        [Fact]
        public void CreateActorExceptionTaskTest()
        {
            Assert.Throws<InvalidOperationException>(() => this.actorFactory.CreateActor<ITestExeptionTaskActor>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true));
        }

        [Fact]
        public void CreateActorExceptionPropertyTest()
        {
            Assert.Throws<InvalidOperationException>(() => this.actorFactory.CreateActor<ITestExeptionPropertyActor>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true));
        }

        [Fact]
        public void CreateActorExceptionClassTest()
        {
            Assert.Throws<InvalidOperationException>(() => this.actorFactory.CreateActor<TestExceptionActor>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true));
        }

        [Fact]
        public void CreateActorExceptionMultipleArgsTest()
        {
            Assert.Throws<InvalidOperationException>(() => this.actorFactory.CreateActor<ITestExeptionMultipleArgsActor>(new Uri("http://qooba.net"), () => this.actorClientMock.Object, true));
        }

        public interface ITestActor
        {
            Task<TestActorResponse> Request(TestActorRequest request);
        }

        public interface ITestActorStr
        {
            Task<string> Request(string request);
        }

        public class TestExceptionActor
        {
            public Task<TestActorResponse> Request(TestActorRequest request)
            {
                return null;
            }
        }

        public interface ITestExeptionTaskActor
        {
            TestActorResponse Request(TestActorRequest request);
        }

        public interface ITestExeptionPropertyActor
        {
            TestActorResponse Request(TestActorRequest request);

            string TestProperty { get; set; }
        }

        public interface ITestExeptionMultipleArgsActor
        {
            TestActorResponse Request(TestActorRequest request, string arg);
        }

        public class TestActorRequest
        {
            public string Request { get; set; }
        }

        public class TestActorResponse
        {
            public string Response { get; set; }
        }

        public static void TestMethod()
        {

        }
    }
}
