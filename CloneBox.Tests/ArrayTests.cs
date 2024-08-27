using FluentAssertions;
using FluentAssertions.Equivalency.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class ArrayTests {

        [Fact]
        public void IntegerArray() {
            var origArr = new[] { 1, 2, 3, 2, 0, 1 };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(6);
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr.Should().NotBeSameAs(origArr);
        }

        [Fact]
        public void NullableIntegerArray() {
            var origArr = new int?[] { 1, 2, null, 3, 2, null, 0, 1 };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(8);
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr.Should().NotBeSameAs(origArr);
        }

        [Fact]
        public void StringArray() {
            var origArr = new[] { "1", "2", "3", null, "", "1", "3", null };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(8);
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr.Should().NotBeSameAs(origArr);
        }

        [Fact]
        public void StringArrayCastedAsObject() {
            // checking that cached object correctly clones arrays of different length
            var origArr = (object)new[] { "1", "2", "3", null, "555", "1" };
            var clonedArr = origArr.CloneX() as string[];
            clonedArr.Should().HaveCount(6);
            clonedArr.Should().BeEquivalentTo(origArr as string[]);
            clonedArr.Should().NotBeSameAs(origArr as string[]);
        }

        [Fact]
        public void ByteArray() {
            // checking that cached object correctly clones arrays of different length
            var origArr = Encoding.ASCII.GetBytes("test test test");
            var clonedArr = origArr.CloneX();
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr.Should().NotBeSameAs(origArr);
        }

        public class OBJ {
            public int? X { get; set; }
            public string Y { get; set; }
        }

        [Fact]
        public void ClassArray() {
            var obj1 = new OBJ { X = 1, Y = "1" };
            var obj2 = new OBJ { X = 2, Y = "2" };
            var origArr = new[] { obj1, obj2, null, obj1 };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(4);
            clonedArr.Should().NotBeSameAs(origArr);
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr[0].Should().NotBeSameAs(origArr[0]);
            clonedArr[0].Should().BeSameAs(clonedArr[3]);
            clonedArr[0].X.Should().Be(1);
            clonedArr[1].X.Should().Be(2);
            clonedArr[2].Should().BeNull();
        }


        public class OBJCON {
            public OBJCON(int x, string y) {
                X = x;
                Y = y;
            }

            public string Y { get; set; }
            public int X { get; set; }
        }

        [Fact]
        public void ClassWithConstructorArray() {
            var origArr = new[] { new OBJCON(1, "1"), new OBJCON(2, "2"), null, new OBJCON(1, "3") };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(4);
            clonedArr.Should().NotBeSameAs(origArr);
            clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr[0].Should().NotBeSameAs(origArr[0]);
            clonedArr[0].Should().NotBeSameAs(clonedArr[3]);
            clonedArr[0].Y.Should().Be("1");
            clonedArr[1].Y.Should().Be("2");
            clonedArr[0].X.Should().Be(1);
            clonedArr[1].X.Should().Be(2);
            clonedArr[2].Should().BeNull();
        }

        public struct STRUCT {
            public string X { get; set; }
            public int? Y { get; set; }
            public OBJCON OBJCON { get; set; }

        }

        [Fact]
        public void StructArray() {
            var rec1 = new STRUCT() { X = "1", Y = 1, OBJCON = new OBJCON(1, "1") };
            var rec2 = new STRUCT() { X = "2", Y = 2, OBJCON = new OBJCON(2, "2") };
            var origArr = new STRUCT[] { rec1, rec2, rec1, rec2 };

            var clonedArr = origArr.CloneX();

            clonedArr.Should().HaveCount(4);
            clonedArr.Should().NotBeSameAs(origArr);
            //clonedArr.Should().BeEquivalentTo(origArr);
            clonedArr[0].Should().NotBeSameAs(origArr[0]);
            clonedArr[0].Should().NotBeSameAs(clonedArr[1]);
            clonedArr[0].Should().NotBeSameAs(clonedArr[2]);

            clonedArr[0].X.Should().Be("1");
            clonedArr[1].X.Should().Be("2");
            clonedArr[0].Y.Should().Be(1);
            clonedArr[1].Y.Should().Be(2);
            clonedArr[0].OBJCON.Y.Should().Be("1");
            clonedArr[1].OBJCON.Y.Should().Be("2");
        }

        public struct STRUCTCON {
            public STRUCTCON(string[] array, STRUCT[] strArray) {
                StringArray = array;
                StrArray = strArray;
            }

            public string[] StringArray;
            public STRUCT[] StrArray { get; set; }
        }



        [Fact]
        public void StructArrayWithConstructor() {
            var srec1 = new STRUCT() { X = "1", Y = 1, OBJCON = new OBJCON(1, "1") };
            var srec2 = new STRUCT() { X = "2", Y = 2, OBJCON = new OBJCON(2, "2") };
            var rec1 = new STRUCTCON(new string[] { "A" }, new STRUCT[] { srec1, srec2 });
            var rec2 = new STRUCTCON(new string[] { "B" }, new STRUCT[] { srec2 }); 
            var origArr = new[] { rec1, rec2  };

            var clonedArr = origArr.CloneX();

            clonedArr.Should().HaveCount(2);
            clonedArr.Should().NotBeSameAs(origArr);
            clonedArr[0].Should().NotBeSameAs(origArr[0]);
            clonedArr[0].Should().NotBeSameAs(clonedArr[1]);

            clonedArr[0].StringArray.Should().BeEquivalentTo(new string[] { "A" });
            clonedArr[1].StringArray.Should().BeEquivalentTo(new string[] { "B" });
            clonedArr[0].StrArray.Should().HaveCount(2);
            clonedArr[0].StrArray[0].OBJCON.X.Should().Be(1);
            clonedArr[0].StrArray[1].OBJCON.X.Should().Be(2);
            clonedArr[1].StrArray.Should().HaveCount(1);
            clonedArr[1].StrArray[0].OBJCON.X.Should().Be(2);
        }

        [Fact]
        public void NullArray() {
            var origArr = new OBJ[] { null, null };
            var clonedArr = origArr.CloneX();
            clonedArr.Should().HaveCount(2);
            clonedArr[0].Should().BeNull();
            clonedArr[1].Should().BeNull();
        }

        [Fact]
        public void NullAsArray() {
            var origArr = (int[])null;
            var clonedArr = origArr.CloneX();
            clonedArr.Should().BeNull();
        }


        [Fact]
        public void ArrayOfSameArrays() {
            var rec1 = new int?[] { 1, null, 3 };
            var origArr = new[] { rec1, rec1, rec1, rec1 };
            var clonedArr = origArr.CloneX();

            clonedArr.Should().HaveCount(4);
            clonedArr.Should().NotBeSameAs(origArr);
            clonedArr.Should().BeEquivalentTo(origArr);

            clonedArr[0].Should().NotBeSameAs(origArr[0]);
            clonedArr[0].Should().BeSameAs(clonedArr[1]);
            clonedArr[0].Should().BeSameAs(clonedArr[2]);
            clonedArr[0].Should().BeSameAs(clonedArr[3]);
           
          
        }

        public class ARROBJ {
            public int[] A { get; set; }

            public int?[] B { get; set; }
        }

        [Fact]
        public void ArrayOfSameClass() {
            var orig = new ARROBJ();
            orig.A = new int[] { 1, 2, 3 };
            orig.B = new int?[] { 1, null, 3 };
            var clone = orig.CloneX();

            clone.Should().NotBeSameAs(orig);
            clone.A.Should().NotBeSameAs(orig.A);
            clone.B.Should().NotBeSameAs(orig.B);

            clone.A.Should().BeEquivalentTo(orig.A);
            clone.B.Should().BeEquivalentTo(orig.B);
        }

        [Fact]
        public void ClassWithNullArrays() {
            var orig = new ARROBJ();
            var clone = orig.CloneX();

            clone.Should().NotBeSameAs(orig);
            clone.A.Should().BeNull();
            clone.B.Should().BeNull();
        }

        [Fact]
        public void ArrayAsNonGenericArray() {
            var arr = new[] { 1, 2, 3 };
            var genArr = (Array)arr;
            var clone = (int[])genArr.CloneX();
            clone.Should().NotBeSameAs(arr);
            clone.Length.Should().Be(3);
            clone[0].Should().Be(1);
            clone[1].Should().Be(2);
            clone[2].Should().Be(3);
        }

        [Fact]
        public void ArrayAsIEnumerable() {
            var arr = new[] { 1, 2, 3 };
            var genArr = (IEnumerable<int>)arr;
            var clone = (int[])genArr.CloneX();
            clone.Should().NotBeSameAs(arr);
            clone.Length.Should().Be(3);
            clone[0].Should().Be(1);
            clone[1].Should().Be(2);
            clone[2].Should().Be(3);
        }

        

        public class CHARARRAY {
            public char[] CharArr { get; set; }
        }

        [Fact]
        public void CharArrayInClass() {
            var orig = new CHARARRAY() {
                CharArr = new[] { 'T', 'e', 's', 't' }
            };
            var clone = orig.CloneX();
            string.Join("", clone.CharArr.Select(c => c.ToString())).Should().Be("Test");
        }


        public class TestClass {
            public int Id { get; set; }
            public string Value { get; set; }
            public TestClass(int id, string value) {
                Id = id;
                Value = value;
            }
            public TestClass() {

            }
        }

        [Fact]
        public void CopyObjectArray() {
            var orig = new object[] { "Test1", new TestClass(1, "Test1"), new DateTime(2022, 1, 1) };
            var clone = orig.CloneX();
            orig.Should().NotBeSameAs(clone);
            (orig.ElementAt(1) as TestClass).Value = "X";
            orig = null;
            Assert.True(typeof(object[]) == clone.GetType());
            Assert.Collection(clone,
                i => Assert.Equal("Test1", i),
                i => Assert.True(i is TestClass && (i as TestClass).Value == "Test1"),
                i => Assert.Equal(new DateTime(2022, 1, 1), i)
            );


        }

    }
}
