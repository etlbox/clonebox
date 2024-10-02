using FluentAssertions;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace CloneBox.Tests {
    public class InstanceCreatorTests {

        public class OBJ {
            public int Id { get; set; }
            public string Value { get; set; }
            public int? NullValue { get; set; }

        }
        [Fact]
        public void BasicTests() {
            CloneXExtensions.CreateInstance<int?>().Should().BeNull();
            CloneXExtensions.CreateInstance<int>().Should().Be(0);
            CloneXExtensions.CreateInstance<string>().Should().Be("");
            CloneXExtensions.CreateInstance<List<string>>().Should().HaveCount(0);
            CloneXExtensions.CreateInstance<Dictionary<string, object>>().Should().HaveCount(0);
            CloneXExtensions.CreateInstance<object>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<OBJ>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<ExpandoObject>().Should().NotBeNull();
            CloneXExtensions.CreateInstance<OBJ[]>().Should().HaveCount(0);
        }

        [Fact]
        public void ObjectTests() {
            var o = CloneXExtensions.CreateInstance<OBJ>();
            o.Id.Should().Be(0);
            o.Value.Should().BeNull();
            o.Id = 3;
            o.Id.Should().Be(3);
        }

    }
}
