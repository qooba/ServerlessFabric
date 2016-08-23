using Qooba.ServerlessFabric.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Qooba.ServerlessFabric.Tests
{
    public class ExpressionHelperTests
    {
        private readonly IExpressionHelper expressionHelper;

        public ExpressionHelperTests()
        {
            this.expressionHelper = new ExpressionHelper();
        }

        [Fact]
        public void CreateActorResponseTypeTest()
        {
            var instance = this.expressionHelper.CreateInstance<TestResponse1Type>();
            Assert.True(instance is TestResponse1Type);
        }

        [Fact]
        public void CreateActorResponseConstructorTypeTest()
        {
            var myName = "MyName";
            var instance = this.expressionHelper.CreateInstance<TestResponse2Type>(myName);
            Assert.True(instance is TestResponse2Type);
            Assert.Equal(instance.Name, myName);
        }

        public class TestResponse1Type
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        public class TestResponse2Type
        {
            public TestResponse2Type(string name)
            {
                this.Name = name;
            }

            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
