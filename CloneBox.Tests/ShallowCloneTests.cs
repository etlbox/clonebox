using System;
using Xunit;

namespace CloneBox.Tests {
    public class CloneTest {

        public class BasicObject : IEquatable<BasicObject> {
            private int _privateIntValue;
            private int _randomIdentifier;
            public bool BoolValue { get; set; }
            public byte ByteValue { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public string StringValue { get; set; }

            public BasicObject() {
                _randomIdentifier = new Random((int)DateTime.UtcNow.Ticks).Next(1, 999999);
            }

            public BasicObject(int privateIntValue) : this() {
                _privateIntValue = privateIntValue;
            }

            public override int GetHashCode() => base.GetHashCode();

            public override bool Equals(object obj) {
                if (obj == null || obj.GetType() != typeof(BasicObject))
                    return false;

                var basicObject = (BasicObject)obj;
                return Equals(basicObject);
            }

            public bool Equals(BasicObject other) {
                if (other == null)
                    return false;
                return
                    other._privateIntValue == _privateIntValue
                    && other._randomIdentifier == _randomIdentifier
                    && other.BoolValue == BoolValue
                    && other.ByteValue == ByteValue
                    && other.IntValue == IntValue
                    && other.LongValue == LongValue
                    && other.StringValue == StringValue;
            }
        }

        [Fact]
        public void ShallowBasicObject() {
            var original = new BasicObject {
                BoolValue = true,
                ByteValue = 0x10,
                IntValue = 100,
                LongValue = 10000,
                StringValue = "Test String"
            };
            var cloned = original.CloneX();

            Assert.Equal(original, cloned);
            Assert.False(original == cloned);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("int")]
        public void WithDataModification(string modificationType) {
            var original = new BasicObject {
                BoolValue = true,
                ByteValue = 0x10,
                IntValue = 100,
                LongValue = 10000,
                StringValue = "Test String"
            };
            var cloned = original.CloneX();
            if (modificationType == "string")
                cloned.StringValue = "A different string";
            else if (modificationType == "bool")
                cloned.BoolValue = false;
            else if (modificationType == "byte")
                cloned.ByteValue = 0x20;
            else if (modificationType == "int")
                cloned.IntValue = 200;

            Assert.NotEqual(original, cloned);
            Assert.False(original == cloned);
        }

        public class SimpleObject {
            public int Id { get; set; }
            public string Value { get; set; }
            public SimpleObject(int id, string value) {
                Id = id;
                Value = value;
            }
            public SimpleObject() {

            }
        }

        [Fact]
        public void ShallowSimpleObject() {
            //Arrange
            var singlePoco = new SimpleObject(1, "Test1");

            //Act
            var copy = singlePoco.CloneX();
            singlePoco.Id = 2;
            singlePoco.Value = "Y";

            //Assert            
            Assert.False(copy.Equals(singlePoco));
            Assert.True(copy.Id == 1);
            Assert.True(copy.Value == "Test1");

        }

    }
}