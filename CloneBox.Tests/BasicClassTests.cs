using FluentAssertions;
using Xunit;

namespace CloneBox.Tests {
    public class BasicClassTests {
               
        public class ClassWithNullable {
            public int? A { get; set; }

            public long? B { get; set; }
        }

        [Fact]
        public void CloneNullables() {
            var c = new ClassWithNullable { B = 42 };
            var cloned = c.CloneX();
            cloned.A.Should().BeNull();
            cloned.B.Should().Be(42);
        }

        public class C1 {
            public C2 C { get; set; }
        }

        public class C2 {
            public int X { get; set; }
        }



        [Fact]
        public void ClassWithObject() {
            var c1 = new C1();
            c1.C = new C2() {  X = 3 };
            var cloned = c1.CloneX();
            cloned.Should().NotBeNull();
            cloned.Should().NotBeSameAs(c1);
            cloned.C.Should().NotBeNull();
            cloned.C.Should().NotBeSameAs(c1.C);
        }

        public class C3 {
            public string X { get; set; }
        }


        public class EmptyClass { }

        [Fact]
        public void CloningEmptyClass() {
            var orig = new EmptyClass();
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Should().NotBeSameAs(orig);
        }


        public class Readonly1 {
            public readonly object X;

            public object Z = new object();

            public Readonly1(string x) {
                X = x;
            }
        }

        [Fact]
        public void IgnoreReadonlyFields() {
            var orig = new Readonly1("Z");
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            orig.X.Should().Be("Z");
            clone.Z.Should().NotBe("Z");
        }


    }
}
