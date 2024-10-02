using FluentAssertions;
using Xunit;

namespace CloneBox.Tests {
    public class BasicStructTests {

        public struct S1 {
            public int A;
        }

        public struct S2 {
            public S3 S;
        }

        public struct S3 {
            public bool B;
        }

        [Fact]
        public void SimpleStruct() {
            var s1 = new S1 { A = 1 };
            var cloned = s1.CloneX();
            cloned.A.Should().Be(1);
        }

        [Fact]
        public void StructWithChild() {
            var s1 = new S2 { S = new S3 { B = true } };
            var cloned = s1.CloneX();
            cloned.S.B.Should().Be(true);
        }

        public struct S4 {
            public C2 C;

            public int F;
        }

        public class C2 {
        }

        [Fact]
        public void StructWithClass() {
            var c1 = new S4();
            c1.F = 1;
            c1.C = new C2();
            var cloned = c1.CloneX();
            c1.F = 2;
            cloned.C.Should().NotBeNull();
            cloned.C.Should().NotBeSameAs(c1.C);
            cloned.F.Should().Be(1);
        }

        public sealed class C6 {
            public readonly int X = 1;

            private readonly object y = new object();

            // Structs can't be null!            

            /* Unmerged change from project 'CloneBox.Tests (net8.0)'
            Before:
                        private readonly StructWithObject z = new StructWithObject();


                        public object GetY() {
            After:
                        private readonly StructWithObject z = new StructWithObject();


                        public object GetY() {
            */

            /* Unmerged change from project 'CloneBox.Tests (net47)'
            Before:
                        private readonly StructWithObject z = new StructWithObject();


                        public object GetY() {
            After:
                        private readonly StructWithObject z = new StructWithObject();


                        public object GetY() {
            */
            private readonly StructWithObject z = new StructWithObject();


            public object GetY() {
                return y;
            }
        }

        public struct StructWithObject {
            public readonly object Z;
        }

        [Fact]
        public void ObjectWithReadonlyStruct() {
            var c = new C6();
            var clone = c.CloneX();
            clone.Should().NotBeSameAs(c);
            clone.X.Should().Be(1);
            clone.GetY().Should().NotBeNull();
            clone.GetY().Should().NotBeSameAs(c.GetY());
        }
    }
}
