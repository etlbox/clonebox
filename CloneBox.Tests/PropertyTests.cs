using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class PropertyTests {

        public class IndexedProperty {

            public Dictionary<int, char> Values { get; set; } = new Dictionary<int, char>();
            public IndexedProperty() {
            }

            public char this[int i] {
                get {
                    return Values[i];
                }
                set {
                    Values[i] = value;
                }
            }
        }

        [Fact]
        public void CharsInDict() {
            var orig = new IndexedProperty();
            orig[0] = 'T';
            orig[1] = 's';
            orig[2] = 't';
            var clone = orig.CloneX();

            clone.Values.Should().NotBeSameAs(orig.Values);
            clone.Values[0].Should().Be('T');
            clone.Values[1].Should().Be('s');
            clone.Values[2].Should().Be('t');
        }


        public class PropertyThrows {

            public object Prop1 { get; set; } = new object();

            private object prop2;

            public object Prop2 {
                get => prop2;
                set => throw new ArgumentNullException();
            }
            public void InitProps() {
                Prop1 = "A";
                prop2 = "B";
            }
        }

        [Fact]
        public void PropsWithException() {
            var orig = new PropertyThrows();
            orig.InitProps();
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Prop1.Should().Be("A");
            clone.Prop2.Should().Be("B");

        }
    }
}
