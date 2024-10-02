using FluentAssertions;
using System;
using Xunit;

namespace CloneBox.Tests {
    public class GenericTests {

        public class BASE {
            public int X { get; set; }
        }

        public class CHILD : BASE {
            public int Y { get; set; }
        }

        public class GENERIC<T> {
            public T Value { get; set; }
        }

        [Fact]
        public void SimpleTuple() {
            var orig = new Tuple<int, int>(1, 2).CloneX();
            var clone = orig.CloneX();

            orig.Should().NotBeSameAs(clone);
            clone.Item1.Should().Be(1);
            clone.Item2.Should().Be(2);
        }

        [Fact]
        public void SimpleTuple7Items() {
            var clone = new Tuple<int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7).CloneX();
            clone.Item7.Should().Be(7);
        }

        [Fact]
        public void TupleWithGeneric() {
            var orig = new Tuple<int, GENERIC<object>>(1, new GENERIC<object>());
            orig.Item2.Value = orig;
            var clone = orig.CloneX();
            orig.Should().NotBeSameAs(clone);
            clone.Item2.Value.Should().Be(clone.Item2.Value);
        }

        [Fact]
        public void PrimitiveGeneric() {
            var orig = new GENERIC<int>();
            orig.Value = 12;
            var clone = orig.CloneX();
            orig.Should().NotBeSameAs(clone);
            clone.Value.Should().Be(12);
        }

        [Fact]
        public void ObjectGeneric() {
            var orig = new GENERIC<object>();
            orig.Value = "12";
            var clone = orig.CloneX();
            orig.Should().NotBeSameAs(clone);
            clone.Value.Should().Be("12");
        }

        [Fact]
        public void TupleWithInheritance() {
            var child = new CHILD { X = 1, Y = 2 };
            var orig = new Tuple<BASE, CHILD>(child, child);
            var clone = orig.CloneX();
            child.X = 42;
            child.Y = 42;

            orig.Should().NotBeSameAs(clone);
            clone.Item1.Should().BeSameAs(clone.Item2);
            clone.Item1.X.Should().Be(1);
            clone.Item2.Y.Should().Be(2);
        }
    }
}
