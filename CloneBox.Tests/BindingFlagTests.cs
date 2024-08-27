using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class BindingFlagTests {

        public class DifferentAccessModifiers {
            public int TestProp1 { get; set; }
            public string TestProp2 { get; private set; }
            private int? TestProp3 { get; set; }
            public int? TestProp3Get => TestProp3;

            private string TestField1;
            public string TestField1Get => TestField1;

            public string TestField2;

            public DifferentAccessModifiers() {
                TestProp1 = 1;
                TestProp2 = "2";
                TestProp3 = 3;
                TestField1 = "4";
                TestField2 = "5";
            }
        }

        [Fact]
        public void TestPropsAndFieldsSimple() {
            DifferentAccessModifiers orig = new DifferentAccessModifiers();
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.TestProp1.Should().Be(1);
            clone.TestProp2.Should().Be("2");
            clone.TestProp3Get.Should().Be(3);
            clone.TestField1Get.Should().Be("4");
            clone.TestField2.Should().Be("5");
        }

        public class ModifierTest {
            public class OBJ {
                public int X { get; set; }
            }
            public int Prop1 { get; set; }
            internal int Prop2 { get; set; }
            protected int? Prop3 { get; set; }
            private int? Prop4 { get; set; }
            public OBJ ObjProp1 { get; set; }
            internal OBJ ObjProp2 { get; set; }
            protected OBJ ObjProp3 { get; set; }
            private OBJ ObjProp4 { get; set; }

            private readonly OBJ ReadOnly1;
            protected readonly OBJ ReadOnly2 = new OBJ() { X = 2 };

            public int SetProp1 { get; private set; }
            public int? SetProp2 { get; } = 3;

            public OBJ ObjField1;

            private OBJ ObjField2;

            public void SetValues() {
                SetProp1 = 1;
                ObjField1 = new OBJ() { X = 1 };
                ObjField2 = new OBJ() { X = 2 };
                ObjProp1 = new OBJ() { X = 1 };
                ObjProp2 = new OBJ() { X = 2 };
                ObjProp3 = new OBJ() { X = 3 };
                ObjProp4 = new OBJ() { X = 4 };
                Prop1 = 1;
                Prop2 = 2;
                Prop3 = 3;
                Prop4 = 4;
            }

            public bool CheckValuesDefaultBinding() {
                return SetProp1 == 1 &&
                    SetProp2 == 3 &&
                    ObjField1.X == 1 &&
                    ObjField2.X == 2 &&
                    ObjProp1.X == 1 &&
                    ObjProp2.X == 2 &&
                    ObjProp3.X == 3 &&
                    ObjProp4.X == 4 &&
                    Prop1 == 1 &&
                    Prop2 == 2 &&
                    Prop3 == 3 &&
                    Prop4 == 4 &&
                    ReadOnly1 == null &&
                    ReadOnly2.X == 2;
                ;
            }

            public bool CheckValuesPublicBinding() {
                return SetProp1 == 1 &&
                    SetProp2 == 3 &&
                    ObjField1.X == 1 &&
                    ObjField2 == null &&
                    ObjProp1.X == 1 &&
                    ObjProp2 == null &&
                    ObjProp3 == null &&
                    ObjProp4 == null &&
                    Prop1 == 1 &&
                    Prop2 == 0 &&
                    Prop3 == null &&
                    Prop4 == null &&
                    ReadOnly1 == null &&
                    ReadOnly2.X == 2;
                ;
            }


        }

        [Fact]
        public void DefaultBindingFlags() {
            var orig = new ModifierTest();
            orig.SetValues();
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.CheckValuesDefaultBinding().Should().BeTrue();
        }


        [Fact]
        public void PublicOnlyBindingFlags() {
            var orig = new ModifierTest();
            orig.SetValues();
            var clone = orig.CloneX(new CloneSettings() {
                IncludeNonPublicFields = false,
                IncludeNonPublicProperties = false,
            });
            clone.Should().NotBeSameAs(orig);
            clone.CheckValuesPublicBinding().Should().BeTrue();
        }

       
    }
}
