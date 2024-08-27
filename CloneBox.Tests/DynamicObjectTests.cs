using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class DynamicObjectTests {

        [Fact]
        public void CopySimpleDynamic() {
            dynamic dyn = new ExpandoObject();
            dyn.Id = 1;
            dyn.Value = "Test1";
            dyn.NullValue = null;

            dynamic copy = CloneXExtensions.CloneX(dyn);
            dyn.Id = 0;
            dyn.Value = "X";

            Assert.NotEqual(copy, dyn);
            Assert.True(copy.Id == 1);
            Assert.True(copy.Value == "Test1");
            Assert.True(copy.NullValue == null);
        }

        public class ComplexClass {
            public int Id { get; set; }
            public string Value { get; set; }
            public decimal? Nullable { get; set; }
            public DateTime? DateTime { get; set; }
            public List<ComplexClass> List { get; set; }
            public ComplexClass[] Array { get; set; }
            public Dictionary<object, ComplexClass> Dictionary { get; set; }
            public ComplexClass Object { get; set; }
            public object Object2 { get; set; }

            public static ComplexClass CreateTestObject() {
                var res = new ComplexClass() {
                    Id = 1,
                    Value = "Test1",
                    Nullable = 1.2m,
                    DateTime = new DateTime(2020, 1, 1),
                    List = new List<ComplexClass>() {
                        new ComplexClass() { Id = 2, Value = "Test2", List = new List<ComplexClass>() {
                            new ComplexClass() { Id = 21, Value ="Test21" },
                            new ComplexClass() { Id = 22, Value ="Test22", List = new List<ComplexClass>() {
                                new ComplexClass() { Id = 221, Value ="Test221" },
                                new ComplexClass() { Id = 222, Value ="Test222" }
                            } }
                        } },
                        new ComplexClass() { Id = 3, Value = "Test3" }

                    },
                    Array = new ComplexClass[] {
                        new ComplexClass() { Id = 4, Value = "Test4" },
                        new ComplexClass() { Id = 5, Value = "Test5" }
                    },
                    Dictionary = new Dictionary<object, ComplexClass>() {
                        { "Key1", new ComplexClass() { Id = 6, Value = "Test6" } },
                        { "Key2", new ComplexClass() { Id = 7, Value = "Test7" } }
                    },
                    Object = new ComplexClass() { Id = 8, Value = "Test8" },
                    Object2 = new ComplexClass() { Id = 9, Value = "Test9" }
                };                
                return res;
            }
        }

        [Fact]
        public void CopyComplexDynamic() {
            dynamic dyn = new ExpandoObject();
            dyn.Id = 1;
            dyn.Complex = ComplexClass.CreateTestObject();
            dyn.Value = "Test1";
            dyn.NullValue = null;
            dynamic nestedExpando = new ExpandoObject();
            nestedExpando.String = "TestNested";
            dyn.Nested = nestedExpando;

            dynamic copy = CloneXExtensions.CloneX(dyn);
            dyn.Id = 0;
            dyn.Value = "X";

            Assert.NotEqual(copy, dyn);
            Assert.True(copy.Id == 1);
            Assert.True(copy.Value == "Test1");
            Assert.True(copy.NullValue == null);
            Assert.True((copy.Complex as ComplexClass).Id == 1);
            Assert.True((copy.Complex as ComplexClass).List.Count() == 2);
            Assert.True((copy.Nested as dynamic).String == "TestNested");
            Assert.NotEqual(copy.Complex, dyn.Complex);
        }

        [Fact]
        public void CopyDynamicMultipleNested() {
            dynamic dyn = new ExpandoObject();
            dyn.Level1 = new ExpandoObject();
            dyn.Level1.Text = "Level1";
            dyn.Level1.Level2 = new ExpandoObject();
            dyn.Level1.Level2.Text = "Level2";
            dyn.Level1.Level2.NullValue = null;
            dyn.Level1.Level2.Complex = ComplexClass.CreateTestObject();
            dyn.Level1.Level2.Level3 = new ExpandoObject();
            dyn.Level1.Level2.Level3.Text = "Level3";
            dyn.Level1.Level2.Level3.Complex = ComplexClass.CreateTestObject();

            dynamic copy = CloneXExtensions.CloneX(dyn);

            Assert.NotEqual(copy, dyn);
            dynamic level1 = (copy as dynamic).Level1;
            dynamic level2 = ((copy as dynamic).Level1 as dynamic).Level2;
            dynamic level3 = (((copy as dynamic).Level1 as dynamic).Level2 as dynamic).Level3;

            Assert.True(level1.Text == "Level1");
            Assert.True(level2.Text == "Level2");
            Assert.True(level2.NullValue == null);
            Assert.True(level3.Text == "Level3");
            Assert.True(level2.Complex is ComplexClass);
            Assert.True(level3.Complex is ComplexClass);
            Assert.True((level2.Complex as ComplexClass).List.Count() == 2);
            Assert.True((level3.Complex as ComplexClass).List.Count() == 2);
            Assert.NotEqual(level2.Complex, (dyn as dynamic).Level1.Level2.Complex);
            Assert.NotEqual(level3.Complex, (dyn as dynamic).Level1.Level2.Level3.Complex);
        }

    }
}
