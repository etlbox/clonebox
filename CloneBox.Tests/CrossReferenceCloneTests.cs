using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class CrossReferenceCloneTests {

        public class A {
            public B RefToB { get; set; }
        }

        public class B {
            public A RefToA { get; set; }
        }

        [Fact]
        public void EnsureCloningOfCrossReference() {
            var aOrig = new A();
            var bOrig = new B();
            aOrig.RefToB = bOrig;
            bOrig.RefToA = aOrig;

            var aClone = aOrig.CloneX();
            Assert.True(aClone != aOrig);
            Assert.True(aClone.RefToB != bOrig);
        }

        public class C {
            public C Child { get; set; }
        }

        [Fact]
        public void EnsureCloningOfSelfReference() {
            var cOrig = new C();
            cOrig.Child = cOrig;

            var cClone = cOrig.CloneX();
            Assert.True(cClone.Child == cClone);
            Assert.True(cClone.Child != cOrig);
        }

    }
}
