using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class InheritanceTests {

        public class BASEOBJ {
            public int X { get; set; }
        }

        public class NEWCHILD : BASEOBJ {
            public new int X { get; set; }
        }

        public class CHILD : BASEOBJ {
            public int Y { get; set; }
        }

        public class CONTAINER {
            public BASEOBJ A { get; set; }

            public BASEOBJ B { get; set; }
        }

        public struct STRUCT {
            public BASEOBJ A { get; set; }

            public BASEOBJ B { get; set; }
        }

     

        [Fact]
        public void CopyingRealInstance() {
            var newChildOrig = new NEWCHILD();
            newChildOrig.X = 2;
            var baseObjOrig = newChildOrig as BASEOBJ;            
            baseObjOrig.X = 1;
            
            var newChildClone = newChildOrig.CloneX();
            BASEOBJ baseObjClone = baseObjOrig.CloneX();

            newChildClone.X.Should().Be(2);
            (newChildClone as BASEOBJ).X.Should().Be(0);
            newChildOrig.Should().NotBeSameAs(newChildClone);

            (baseObjClone as NEWCHILD).X.Should().Be(2);
            baseObjOrig.Should().NotBeSameAs(baseObjClone);
        }

        [Fact]
        public void ParentPropertyCloning() {
            var childOrig = new CHILD();
            childOrig.Y = 2;
            childOrig.X = 1;
            var childClone = childOrig.CloneX();
            childClone.Y.Should().Be(2);
            childClone.X.Should().Be(1);
            childOrig.Should().NotBeSameAs(childClone);            
        }

        [Fact]
        public void IgnoreCastingToParent() {
            var childOrig = new CHILD();
            childOrig.Y = 2;
            childOrig.X = 1;
            var parent = childOrig as BASEOBJ;
            childOrig.X = 3;
            var parentClone = parent.CloneX();
            var childClone = childOrig.CloneX();
            
            parentClone.Should().NotBeSameAs(parent);
            childClone.Should().NotBeSameAs(childOrig);
            parentClone.X.Should().Be(3);            
            childClone.Y.Should().Be(2);
            childClone.X.Should().Be(3);
        }

        [Fact]
        public void DeepCopyOfClass() {
            var containerOrig = new CONTAINER();
            var baseObjOrig = new BASEOBJ { X = 1 };
            containerOrig.A = baseObjOrig;
            containerOrig.B = baseObjOrig;
            var containerClone = containerOrig.CloneX();
            baseObjOrig.X = 2;
            containerClone.A.X.Should().Be(1);
            containerClone.B.X.Should().Be(1);
            containerClone.A.Should().Be(containerClone.B);
        }

        [Fact]
        public void DeepCopyOfStruct() {
            var structOrig = new STRUCT();
            var baseObjOrig = new BASEOBJ { X = 1 };
            structOrig.A = baseObjOrig;
            structOrig.B = baseObjOrig;
            var structClone = structOrig.CloneX();
            baseObjOrig.X = 2;
            structClone.A.X.Should().Be(1);
            structClone.B.X.Should().Be(1);
            structClone.A.Should().Be(structClone.B);
        }

        [Fact]
        public void DeepCopyOfArray() {
            var child1 = new CHILD { X = 1, Y = 2 };
            var child2 = new CHILD { X = 1, Y = 3 };
            var arrayOrig = new[] { child1, child2,child1 };

            var arrayClone = arrayOrig.CloneX();
            arrayClone.Length.Should().Be(3);
            arrayClone[0].X.Should().Be(1);
            arrayClone[1].X.Should().Be(1);
            arrayClone[2].X.Should().Be(1);
            arrayClone[0].Y.Should().Be(2);
            arrayClone[1].Y.Should().Be(3);
            arrayClone[2].Y.Should().Be(2);
            arrayClone[0].Should().Be(arrayClone[2]);
            arrayClone[0].Should().NotBe(arrayClone[1]);
        }

        public class CLASSINTERFACE : IDisposable {
            public int X;

            public object O; // make it not safe

            public void Dispose() {
            }
        }


        public struct STRUCTINTERFACE : IDisposable {
            public IDisposable X { get; set; }

            public void Dispose() {
            }
        }

        [Fact]
        public void StructCastedToInterface() {
            var s = new STRUCTINTERFACE();
            s.X = new CLASSINTERFACE() { X = 3 };
            var orig = s as IDisposable;
            var clone = orig.CloneX();
            s.X = null;

            clone.Should().BeOfType(typeof(STRUCTINTERFACE));            
            ((STRUCTINTERFACE)clone).X.Should().BeOfType(typeof(CLASSINTERFACE));
            ((CLASSINTERFACE)((STRUCTINTERFACE)clone).X).X.Should().Be(3);
            clone.Should().NotBeSameAs(orig);
        }

        [Fact]
        public void ClassCastedToInterface() {
            var bo = new CLASSINTERFACE() { X = 3, O = new STRUCTINTERFACE() };
            var orig = bo as IDisposable;
            var clone = orig.CloneX();
            clone.Should().BeOfType(typeof(CLASSINTERFACE));
            ((CLASSINTERFACE)clone).O.Should().BeOfType(typeof(STRUCTINTERFACE));
            clone.Should().NotBeSameAs(orig);

        }

        [Fact]
        public void ClassCastedToObject() {
            var bo = new BASEOBJ() { X = 3 };
            var orig = bo as object;
            var clone = orig.CloneX();
            bo.X = 2;

            clone.Should().BeOfType(typeof(BASEOBJ));
            clone.Should().NotBeSameAs(orig);
        }

        [Fact]
        public void ArrayOfCastedInterfaces() {
            var c1 = new CLASSINTERFACE() { X = 1 };
            var c2 = new CLASSINTERFACE() { X = 2 };
            var orig = new IDisposable[] { c1, c2,c1 };
            var clone = orig.CloneX();
            clone.Should().HaveCount(3);
            clone.Should().NotBeSameAs(orig);
            clone[0].Should().BeOfType(typeof(CLASSINTERFACE));
            (clone[0] as CLASSINTERFACE).X.Should().Be(1);
            (clone[1] as CLASSINTERFACE).X.Should().Be(2);
            clone[2].Should().BeSameAs(clone[0]);
        }


        public class VirtualClass1 {
            public virtual int A { get; set; }

            public virtual int B { get; set; }

            public object X { get; set; }
        }

        public class VirtualClass2 : VirtualClass1 {
            public override int B { get; set; }
        }

        [Fact]
        public void ClassWithVirtualProps() {
            var v2 = new VirtualClass2();
            v2.A = 1;
            v2.B = 2;
            var v1 = v2 as VirtualClass1;
            v1.A = 3;
            var clone = v1.CloneX() as VirtualClass2;
            v2.B = 0;
            v2.A = 0;
            clone.Should().NotBeSameAs(v1);
            clone.B.Should().Be(2);
            clone.A.Should().Be(3);
        }

    }
}
