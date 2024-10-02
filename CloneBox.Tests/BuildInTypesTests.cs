using FluentAssertions;
using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace CloneBox.Tests {
    public class BuildInTypesTests {

        //Documentation of build-in types:
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
        public class BuildInDataTypes : IEquatable<BuildInDataTypes> {
            System.Boolean _boolType;
            System.Byte _byteType;
            System.SByte _sbyteType;
            System.Char _charType;
            System.Decimal _decimalType;
            System.Double _doubleType;
            System.Single _floatType;
            System.Int32 _intType;
            System.UInt32 _uintType;
            System.Int64 _longType;
            System.UInt64 _ulongType;
            System.Int16 _shortType;
            System.UInt16 _ushortType;

            public bool BoolProp { get; set; }
            public byte ByteProp { get; set; }
            public sbyte SbyteProp { get; set; }
            public char CharProp { get; set; }
            public decimal DecimalProp { get; set; }
            public double DoubleProp { get; set; }
            public float FloatProp { get; set; }
            public int IntProp { get; set; }
            public uint UintProp { get; set; }
            public long LongProp { get; set; }
            public ulong UlongProp { get; set; }
            public short ShortProp { get; set; }
            public ushort UshortProp { get; set; }


            public override int GetHashCode() => base.GetHashCode();

            public override bool Equals(object obj) {
                if (obj == null || obj.GetType() != typeof(BuildInDataTypes))
                    return false;
                return Equals((BuildInDataTypes)obj);
            }

            public bool Equals(BuildInDataTypes other) {
                if (other == null)
                    return false;
                return
                    other._boolType == _boolType &&
                    other._byteType == _byteType &&
                    other._sbyteType == _sbyteType &&
                    other._charType == _charType &&
                    other._decimalType == _decimalType &&
                    other._doubleType == _doubleType &&
                    other._floatType == _floatType &&
                    other._intType == _intType &&
                    other._uintType == _uintType &&
                    other._longType == _longType &&
                    other._ulongType == _ulongType &&
                    other._shortType == _shortType &&
                    other._ushortType == _ushortType &&
                    other.BoolProp == BoolProp &&
                    other.ByteProp == ByteProp &&
                    other.SbyteProp == SbyteProp &&
                    other.CharProp == CharProp &&
                    other.DecimalProp == DecimalProp &&
                    other.DoubleProp == DoubleProp &&
                    other.FloatProp == FloatProp &&
                    other.IntProp == IntProp &&
                    other.UintProp == UintProp &&
                    other.LongProp == LongProp &&
                    other.UlongProp == UlongProp &&
                    other.ShortProp == ShortProp &&
                    other.UshortProp == UshortProp;
            }

            public static BuildInDataTypes CreateTestObject() {
                return new BuildInDataTypes() {
                    _boolType = true,
                    _byteType = 0x10,
                    _sbyteType = 0x20,
                    _charType = 'A',
                    _decimalType = 100.0m,
                    _doubleType = 100.0,
                    _floatType = 100.0f,
                    _intType = 100,
                    _uintType = 100,
                    _longType = 100,
                    _ulongType = 100,
                    _shortType = 100,
                    _ushortType = 100,
                    BoolProp = true,
                    ByteProp = 0x30,
                    SbyteProp = 0x40,
                    CharProp = 'B',
                    DecimalProp = 200.0m,
                    DoubleProp = 200.0,
                    FloatProp = 200.0f,
                    IntProp = 200,
                    UintProp = 200,
                    LongProp = 200,
                    UlongProp = 200,
                    ShortProp = 200,
                    UshortProp = 200
                };
            }
        }

        [Fact]
        public void CLRDataTypesTests() {
            var original = BuildInDataTypes.CreateTestObject();
            var cloned = original.CloneX();

            Assert.Equal(original, cloned);
            Assert.False(original == cloned);
        }

        public class SpecialTypes {
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }

            public IntPtr IntPtr { get; set; }

            public UIntPtr UIntPtr { get; set; }

            public AttributeTargets Enum { get; set; }

            public Guid Guid { get; set; }
        }

        [Fact]
        public void SpecialTypesTests() {
            var orig = new SpecialTypes {
                DateTime = new DateTime(2001, 01, 01),
                DateTimeOffset = new DateTime(2001, 01, 01).ToUniversalTime(),
                IntPtr = new IntPtr(42),
                UIntPtr = new UIntPtr(42),
                Enum = AttributeTargets.Delegate,
                Guid = Guid.NewGuid()
            };

            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.DateTime.Should().Be(new DateTime(2001, 1, 1));
            clone.DateTimeOffset.Should().Be(new DateTime(2001, 1, 1));
            clone.IntPtr.Should().Be(new IntPtr(42));
            clone.UIntPtr.Should().Be(new UIntPtr(42));
            clone.Enum.Should().Be(AttributeTargets.Delegate);
            clone.Guid.Should().NotBeEmpty();
        }



        [Fact]
        public void PrimitiveClones() {
            3.CloneX().Should().Be(3);
            'x'.CloneX().Should().Be('x');
            "xxxxxxxxxx yyyyyyyyyyyyyy".CloneX().Should().Be("xxxxxxxxxx yyyyyyyyyyyyyy");
            string.Empty.CloneX().Should().Be(string.Empty);
            true.CloneX().Should().Be(true);
            DateTime.MinValue.CloneX().Should().Be(DateTime.MinValue);
            AttributeTargets.Delegate.CloneX().Should().Be(AttributeTargets.Delegate);
            ((object)null).CloneX().Should().BeNull();
            var obj = new object();
            obj.CloneX().Should().NotBeNull();
            obj.CloneX().GetType().Should().Be(typeof(object));
            obj.CloneX().Should().NotBeSameAs(obj);
        }



        [Fact]
        public void StringBuilderClone() {
            var orig = new StringBuilder();
            orig.Append("test1");
            var clone = orig.CloneX();
            clone.ToString().Should().Be("test1");

            /* Unmerged change from project 'CloneBox.Tests (net8.0)'
            Before:
                    }


                    [Fact]
            After:
                    }


                    [Fact]
            */

            /* Unmerged change from project 'CloneBox.Tests (net47)'
            Before:
                    }


                    [Fact]
            After:
                    }


                    [Fact]
            */
        }


        [Fact]
        public void ActionClone() {
            var closure = new[] { "123" };
            Action<int> orig = (n) => {
                closure[0].Should().Be("123");
                closure[0] += n.ToString();
            };
            var clone = orig.CloneX();
            orig(10);
            clone(100);

            clone.Should().NotBeNull();
            closure[0].Should().Be("12310");
        }

        //https://stackoverflow.com/questions/22151871/deep-copying-a-func-within-an-object-in-c-sharp
        [Fact]
        public void FuncsClone() {
            var closure = new[] { "123" };
            Func<int, string> orig = x => closure[0] + x.ToString(CultureInfo.InvariantCulture);
            var clone = orig.CloneX();
            closure[0] = "xxx";
            clone.Should().NotBeNull();
            orig(3).Should().Be("xxx3");
            clone(3).Should().Be("1233");
        }


        public class EventHandlerTest1 {
            public event Action<int> Event;

            public int Call(int x) {
                if (Event != null) {
                    Event(x);
                    return Event.GetInvocationList().Length;
                }

                return 0;
            }
        }

        [Fact]
        public void EventsClone() {
            var orig = new EventHandlerTest1();
            var summ = new int[1];
            Action<int> a1 = x => summ[0] += x;
            Action<int> a2 = x => summ[0] += x;
            orig.Event += a1;
            orig.Event += a2;
            orig.Call(1);
            summ[0].Should().Be(2);
            var clone = orig.CloneX();
            clone.Call(1);
            summ[0].Should().Be(4);
            orig.Event -= a1;
            orig.Event -= a2;
            orig.Call(1).Should().Be(0);
            summ[0].Should().Be(4);
            clone.Call(1).Should().Be(2);
        }

        [Fact]
        public void DbNullClone() {
            //DBNull must be created as a value type
            var orig = DBNull.Value;
            var clone = orig.CloneX();
            clone.CloneX().Should().Be(DBNull.Value);
        }


    }
}
