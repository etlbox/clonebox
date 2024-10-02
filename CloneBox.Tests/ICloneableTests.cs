using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CloneBox.Tests {
    public class ICloneableTests {

        public class CloneableClass : ICloneable {
            public int Id { get; set; }
            public string Value { get; set; }

            public object Clone() {
                return new CloneableClass() { Id = 2, Value = "X" };
            }
        }

        [Fact]
        public void ImplementingICloneable() {
            var orig = new CloneableClass();

            var clone = orig.CloneX(new CloneSettings() {
                PreferICloneable = true
            });
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(2);
            clone.Value.Should().Be("X");

        }

        [Fact]
        public void ListOfICloneable() {
            var elementA = new CloneableClass() { Id = 1, Value = "A" };
            var elementB = new CloneableClass() { Id = 3, Value = "C" };
            var orig = new List<CloneableClass>() {
                elementA, elementB, elementA
            };

            var clone = orig.CloneX(new CloneSettings() {
                PreferICloneable = true
            });
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(3);
            clone.ElementAt(0).Id.Should().Be(2);
            clone.ElementAt(1).Id.Should().Be(2);
            clone.ElementAt(1).Should().NotBeSameAs(clone.ElementAt(0));
            clone.ElementAt(2).Should().BeSameAs(clone.ElementAt(0));
        }

        public class CloneableContainer {
            public CloneableClass CloneableA { get; set; }
            public CloneableClass CloneableB { get; set; }
        }

        [Fact]
        public void CloneableContainerClass() {
            var orig = new CloneableClass();
            var container = new CloneableContainer() {
                CloneableA = orig,
                CloneableB = orig
            };
            var containerClone = container.CloneX(new CloneSettings() {
                PreferICloneable = true
            });
            containerClone.CloneableA.Should().NotBeSameAs(orig);
            containerClone.CloneableA.Should().BeSameAs(containerClone.CloneableB);
            containerClone.CloneableA.Id.Should().Be(2);

        }

    }
}
