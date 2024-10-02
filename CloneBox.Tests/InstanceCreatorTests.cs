using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Globalization;
using System.Text;
using Xunit;
using static CloneBox.Tests.DynamicObjectTests;

namespace CloneBox.Tests {
    public class InstanceCreatorTests {

        [Fact]
        public void BasicTests() {
            CloneXExtensions.CreateInstance<int?>().Should().BeNull();
            CloneXExtensions.CreateInstance<int>().Should().Be(0);
            CloneXExtensions.CreateInstance<string>().Should().Be("");
            CloneXExtensions.CreateInstance<List<string>>().Should().HaveCount(0);
            CloneXExtensions.CreateInstance<Dictionary<string,object>>().Should().HaveCount(0);
            CloneXExtensions.CreateInstance<object>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<POCO>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<ExpandoObject>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<POCO[]>().Should().HaveCount(0);
        }
     
    }
}
