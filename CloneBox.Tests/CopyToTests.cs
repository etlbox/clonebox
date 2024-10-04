using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class CopyToTests {

        public class BASE {
            public int A { get; set; }

            public virtual string B { get; set; }

            public byte[] C { get; set; }
        }


        [Fact]
        public void SimpleCopyTo() {
            var cFrom = new BASE {
                A = 12,
                B = "testestest",
                C = new byte[] { 1, 2, 3 }
            };

            var cTo = new BASE {
                A = 11,
                B = "tes",
                C = new byte[] { 1 }
            };

            var cToRef = cTo;

            cFrom.CloneXTo(cTo);

            ReferenceEquals(cTo, cToRef).Should().BeTrue();
            cTo.A.Should().Be(12);
            cTo.B.Should().Be("testestest");
            cTo.C.Length.Should().Be(3);
            cTo.C[2].Should().Be(3);
        }

        public class CHILDNEW : BASE {
            public decimal D { get; set; }

            public new int A { get; set; }
        }

        [Fact]
        public void CopyChildProperties() {
            var cFrom = new BASE {
                A = 12,
                B = "testestest",
                C = new byte[] { 1, 2, 3 }
            };

            var cTo = new CHILDNEW {
                A = 11,
                D = 42.3m
            };

            var cToRef = cTo;

            cFrom.CloneXTo(cTo);

            ReferenceEquals(cTo, cToRef).Should().BeTrue();
            cTo.A.Should().Be(12);
            ((BASE)cTo).A.Should().Be(0);
            cTo.D.Should().Be(42.3m);
        }

        public class BASECONTAINER {
            public BASE Base1 { get; set; }

            public BASE Base2 { get; set; }
        }

        [Fact]
        public void ClassWithSubclass() {
            var baseObj = new BASE { A = 12 };
            var cFrom = new BASECONTAINER { Base1 = baseObj, Base2 = baseObj };
            var cTarget = new BASECONTAINER();
            var cTo = cFrom.CloneXTo(cTarget);
            cTarget.Should().BeSameAs(cTo);
            ReferenceEquals(cFrom.Base1, cTo.Base1).Should().BeFalse();
            ReferenceEquals(cFrom.Base2, cTo.Base2).Should().BeFalse();
            ReferenceEquals(cTo.Base1, cTo.Base2).Should().BeTrue();
            cTo.Base1.A.Should().Be(12);
            cTo.Base2.A.Should().Be(12);
        }

        [Fact]
        public void CopyToNull() {
            var baseObj = new BASE();
            var nullclone = baseObj.CloneXTo((BASE)null);
            nullclone.Should().BeNull();
        }

        [Fact]
        public void CopyFromNull() {
            BASE baseObj = null;
            Assert.Throws<InvalidOperationException>(() => {
                baseObj.CloneXTo(new BASE());
            });
        }

        [Fact]
        public void CloneFromNull() {
            BASE baseObj = null;
            baseObj.CloneX().Should().BeNull();
        }

        public class BASECHILD : BASE { }


        [Fact]
        public void DifferentInheritance() {
            BASE baseObj = new BASECHILD {
                A = 12,
                B = "testestest",
                C = new byte[] { 1, 2, 3 }
            };
            CHILDNEW childObj = new CHILDNEW();

            var clone = baseObj.CloneXTo(childObj);

            clone.Should().BeSameAs(childObj);
            clone.A.Should().Be(12);
            clone.B.Should().Be("testestest");
            clone.C.Length.Should().Be(3);
            clone.C[2].Should().Be(3);
        }

        public interface I1 {
            int A { get; set; }
        }

        public struct S1 : I1 {
            public int A { get; set; }
        }

        [Fact]
        public void StructAsInterface() {
            S1 sFrom = new S1 { A = 42 };
            S1 sTo = new S1();
            var objTo = (I1)sTo;
            objTo.A = 23;
            var clone = ((I1)sFrom).CloneXTo(objTo);
            clone.Should().BeSameAs(objTo);
            clone.A.Should().Be(42);
        }


        [Fact]
        public void StringIntoString() {
            var s1 = "abc";
            var s2 = "def";
            var s3 = s1.CloneXTo(s2);
            s1.Should().Be("abc");
            s2.Should().Be("def");
            s3.Should().Be("abc");
        }

        [Fact]
        public void ArraySameSize() {
            var arrFrom = new[] { 1, 2, 3 };
            var arrTo = new[] { 4, 5, 6 };
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(3);
            arrTo[0].Should().Be(1);
            arrTo[1].Should().Be(2);
            arrTo[2].Should().Be(3);
        }

        [Fact]
        public void BigArrayIntoSmallerArray() {
            var arrFrom = new[] { 1, 2, 3 };
            var arrTo = new[] { 4, 5 };
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(2);
            arrTo[0].Should().Be(1);
            arrTo[1].Should().Be(2);
        }

        [Fact]
        public void SmallArrayIntoBiggerArray() {
            var arrFrom = new[] { 1, 2 };
            var arrTo = new[] { 4, 5, 6 };
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(3);
            arrTo[0].Should().Be(1);
            arrTo[1].Should().Be(2);
            arrTo[2].Should().Be(6);
        }

        [Fact]
        public void ObjectArray() {
            var baseObj = new BASECHILD();
            var cont = new BASECONTAINER { Base1 = baseObj, Base2 = baseObj };
            var arrFrom = new[] { cont, cont, cont };
            var arrTo = new BASECONTAINER[4];
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(4);
            arrTo[0].Should().NotBeSameAs(baseObj);
            arrTo[0].Should().BeSameAs(arrTo[1]);
            arrTo[1].Should().BeSameAs(arrTo[2]);
            arrTo[2].Base1.Should().NotBeSameAs(baseObj);
            arrTo[2].Base1.Should().BeSameAs(arrTo[2].Base2);
            arrTo[3].Should().BeNull();
        }

        [Fact]
        public void NonZeroBasedArray() {
            var arrFrom = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 });
            var arrTo = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 0 });
            arrFrom.SetValue(1, 1);
            arrFrom.SetValue(2, 2);
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(2);
            arrTo.GetValue(0).Should().Be(0);
            arrTo.GetValue(1).Should().Be(1);
        }

        [Fact]
        public void MultiDimArray() {
            var arrFrom = Array.CreateInstance(typeof(int), new[] { 2, 2 }, new[] { 1, 1 });
            var arrTo = Array.CreateInstance(typeof(int), new[] { 2, 2 }, new[] { 1, 1 });
            arrFrom.SetValue(1, 1, 1);
            arrFrom.SetValue(2, 2, 2);
            arrFrom.CloneXTo(arrTo);
            arrTo.Length.Should().Be(4);
            arrTo.GetValue(1, 1).Should().Be(1);
            arrTo.GetValue(2, 2).Should().Be(2);
        }

        [Fact]
        public void TwoDimArray() {
            var arrFrom = new[,] { { 1, 2 }, { 3, 4 } };
            var arrTo = new int[3, 1];
            arrFrom.CloneXTo(arrTo);

            arrTo[0, 0].Should().Be(1);
            arrTo[1, 0].Should().Be(3);
            arrTo[2, 0].Should().Be(0);
        }

        [Fact]
        public void DictionaryIntoOther() {
            var dsource = new Dictionary<string, string> { { "A", "B" }, { "C", "D" } };
            var dtarget = new Dictionary<string, string>();
            dsource.CloneXTo(dtarget);
            dsource["A"] = "E";

            dtarget.Count.Should().Be(2);
            dtarget["A"].Should().Be("B");
            dtarget["C"].Should().Be("D");
        }


        public class D1 {
            public int A { get; set; }
        }

        public class D2 : D1 {
            public int B { get; set; }

            public D2(D1 d1) {
                B = 14;
                d1.CloneXTo(this);
            }
        }

        [Fact]
        public void UsingThisKeywordAsTarget() {
            var baseObject = new D1 { A = 12 };
            var wrapper = new D2(baseObject);
            wrapper.A.Should().Be(12);
            wrapper.B.Should().Be(14);
        }
    }
}
