using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class ConstructorTests {

        public class CLONEABLECLASS : ICloneable {
            public object X { get; set; }

            public object Clone() {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void ClonerShouldNotCallAnyMethodOfClonableClass() {
            // just for check, ensure no hidden behaviour in MemberwiseClone            
            var orig = new CLONEABLECLASS();
            var orig2 = new { X = new CLONEABLECLASS() };            
            var clone = orig.CloneX();
            var clone2 = orig2.CloneX();
            orig.Should().NotBeSameAs(clone);
            orig2.Should().NotBeSameAs(clone2);
        }

        public class PRIVATECONST {
            private PRIVATECONST() {
            }

            public static PRIVATECONST Create() {
                return new PRIVATECONST();
            }

            public int Value { get; set; }
        }

        [Fact]
        public void ObjectWithPrivateConstructor() {
            var orig = PRIVATECONST.Create();
            orig.Value = 42;
            var clone = orig.CloneX();
            orig.Value = 0;

            clone.Should().NotBeSameAs(orig);
            clone.Value.Should().Be(42);
        }

        public class COMPLEXCONSTR {
            public COMPLEXCONSTR(int arg1, string arg2, PRIVATECONST arg3) {
            }

            public int Value { get; set; }
        }

        [Fact]
        public void ObjectWithComplexConstructor() {
            var orig = new COMPLEXCONSTR(1, "A", PRIVATECONST.Create());
            orig.Value = 42;
            var clone = orig.CloneX();
            orig.Value = 0;

            clone.Should().NotBeSameAs(orig);
            clone.Value.Should().Be(42);
        }


        [Fact]
        public void AnonymousObject() {
            var orig = new { A = 1, B = "x", C= PRIVATECONST.Create() };
            orig.C.Value = 42;
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.A.Should().Be(1);
            clone.B.Should().Be("x");
            clone.C.Value.Should().Be(42);
        }

        private class CONTEXTBOUNDOBJ : ContextBoundObject {
        }

        
        [Fact]
        public void ContextBound_Object_Should_Be_Cloned() {
            // FormatterServices.CreateUninitializedObject cannot use context-bound objects
            var orig = new CONTEXTBOUNDOBJ();
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            orig.Should().NotBeSameAs(clone);            
        }

        private class MARSHALOBJ : MarshalByRefObject {
        }


        [Fact]
        public void MarshalByRef_Object_Should_Be_Cloned() {
            var orig = new MARSHALOBJ();
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            orig.Should().NotBeSameAs(clone);
        }

        public class ExClass {
            public ExClass() {
                throw new Exception();
            }

            public ExClass(string x) {
                // does not throw here
            }

            public override bool Equals(object obj) {
                //throw new Exception();
                //This is needed for clone dictionary !
                return base.Equals(obj);
            }

            public override int GetHashCode() {
                //throw new Exception(); 
                //This is needed for clone dictionary !
                return base.GetHashCode();
            }

            public override string ToString() {
                throw new Exception();
            }
        }

        [Fact]
        public void ClonerShouldNotCallAnyMethodOfClass() {
            var orig = new ExClass("x");
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            orig.Should().NotBeSameAs(clone);

            var exClass = new ExClass("x");
            var orig2 = new[] { exClass, exClass }.CloneX();
            var clone2 = orig2.CloneX();
            clone2.Should().NotBeNull();
            orig2.Should().NotBeSameAs(clone2);
        }

        public class MULTIPLECONST {

            public MULTIPLECONST() {
                throw new Exception();
            }

            public MULTIPLECONST(int arg1) {
                throw new Exception();
            }

            public MULTIPLECONST(int arg1, string arg2, CLONEABLECLASS arg3) {
            }

            public MULTIPLECONST(int arg1, string arg2) : this(arg1) {

            }

            public int Value { get; set; }
        }

        [Fact]
        public void ObjectWithMultipleConstructors() {
            var orig = new MULTIPLECONST(1, "A", new CLONEABLECLASS());
            orig.Value = 42;
            var clone = orig.CloneX();
            orig.Value = 0;

            clone.Should().NotBeSameAs(orig);
            clone.Value.Should().Be(42);
        }

        [Fact]
        public void OnlyPublicConstructor() {

            var orig = PRIVATECONST.Create();
            orig.Value = 42;
            var clone = orig.CloneX(new CloneSettings() {
                IncludeNonPublicConstructors = false
            });
            clone.Should().BeNull();
        }
    }
}
