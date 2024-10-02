using FluentAssertions;
using Xunit;

namespace CloneBox.Tests {
    public class DoNotCloneTests {

        public class CheckIgnoreAttribute {
            public string Prop1 { get; set; }
            [DoNotClone]
            public string Prop2 { get; set; }
            public string Prop3 { get; set; }
            [DoNotClone]
            public string Prop4 { get; set; }
            public string Prop5 { get; set; }
            public string Field1;
            [DoNotClone]
            public string Field2;
        }

        [Fact]
        public void CheckDefaultAttribute() {

            CheckIgnoreAttribute orig = new CheckIgnoreAttribute() {
                Prop1 = "1",
                Prop2 = "2",
                Prop3 = "3",
                Prop4 = "4",
                Prop5 = "5",
                Field1 = "6",
                Field2 = "7"

            };

            var clone = orig.CloneX();  

            clone.Prop1.Should().Be("1");
            clone.Prop2.Should().BeNull();
            clone.Prop3.Should().Be("3");
            clone.Prop4.Should().BeNull();
            clone.Prop5.Should().Be("5");
            clone.Field1.Should().Be("6");
            clone.Field2.Should().BeNull();
        }

    }
}
