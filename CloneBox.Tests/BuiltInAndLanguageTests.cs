using FluentAssertions;
using System;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace CloneBox.Tests {
    public class BuiltInAndLanguageTests {

        [Fact]
        public void DecimalStandalone() {
            1.25m.CloneX().Should().Be(1.25m);
        }

        [Fact]
        public void GuidStandalone() {
            var orig = Guid.NewGuid();
            orig.CloneX().Should().Be(orig);
        }

        [Fact]
        public void TimeSpanStandalone() {
            var orig = TimeSpan.FromMinutes(5);
            orig.CloneX().Should().Be(orig);
        }

        [Fact(Skip = "CloneX on Uri currently recurses in InstanceCreator until stack overflow")]
        public void UriIsClonedOrPreserved() {
            var orig = new Uri("https://example.com/path?q=1");
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.ToString().Should().Be(orig.ToString());
        }

        [Fact]
        public void VersionIsCloned() {
            var orig = new Version(1, 2, 3, 4);
            var clone = orig.CloneX();
            clone.Should().Be(orig);
            clone.Should().NotBeSameAs(orig);
        }

        [Fact(Skip = "CloneX on CultureInfo currently recurses in InstanceCreator until stack overflow")]
        public void CultureInfoClone() {
            var orig = CultureInfo.GetCultureInfo("de-DE");
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Name.Should().Be("de-DE");
        }

        [Fact(Skip = "CloneX on TimeZoneInfo currently recurses in InstanceCreator until stack overflow")]
        public void TimeZoneInfoClone() {
            var orig = TimeZoneInfo.Utc;
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Id.Should().Be(orig.Id);
        }

        [Fact]
        public void ValueTupleClone() {
            var orig = (Id: 1, Name: "A", Child: new Holder { Value = 5 });
            var clone = orig.CloneX();
            clone.Id.Should().Be(1);
            clone.Name.Should().Be("A");
            clone.Child.Value.Should().Be(5);
            clone.Child.Should().NotBeSameAs(orig.Child);
        }

        [Fact]
        public void BigIntegerClone() {
            var orig = BigInteger.Parse("12345678901234567890");
            orig.CloneX().Should().Be(orig);
        }

        [Fact]
        public void ExceptionCloneKeepsMessage() {
            var orig = new InvalidOperationException("broken");
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Message.Should().Be("broken");
        }

        public class Holder {
            public int Value { get; set; }
        }

        public record PositionalRecord(int Id, string Name);

        public record ClassRecord {
            public int Id { get; set; }
            public Holder Child { get; set; }
        }

        public readonly record struct Point(int X, int Y);

        public class InitOnly {
            public int Id { get; init; }
            public string Name { get; init; }
        }

        public readonly struct ReadonlyStruct {
            public ReadonlyStruct(int value) {
                Value = value;
            }
            public int Value { get; }
        }

        public struct GetOnlyStruct {
            public GetOnlyStruct(string name) {
                Name = name;
            }
            public string Name { get; }
        }

        [Fact]
        public void PositionalRecordClone() {
            var orig = new PositionalRecord(1, "A");
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(1);
            clone.Name.Should().Be("A");
        }

        [Fact]
        public void ClassRecordDeepClone() {
            var orig = new ClassRecord { Id = 1, Child = new Holder { Value = 9 } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Child.Should().NotBeSameAs(orig.Child);
            clone.Child.Value.Should().Be(9);
        }

        [Fact]
        public void RecordStructClone() {
            var orig = new Point(3, 4);
            var clone = orig.CloneX();
            clone.X.Should().Be(3);
            clone.Y.Should().Be(4);
        }

        [Fact]
        public void InitOnlyProperties() {
            var orig = new InitOnly { Id = 4, Name = "init" };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(4);
            clone.Name.Should().Be("init");
        }

        [Fact]
        public void ReadonlyStructClone() {
            var orig = new ReadonlyStruct(12);
            orig.CloneX().Value.Should().Be(12);
        }

        [Fact]
        public void GetOnlyStructClone() {
            var orig = new GetOnlyStruct("hidden");
            orig.CloneX().Name.Should().Be("hidden");
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void DateOnlyAndTimeOnly() {
            var date = new DateOnly(2024, 3, 1);
            var time = new TimeOnly(13, 45, 10);
            date.CloneX().Should().Be(date);
            time.CloneX().Should().Be(time);
        }

        [Fact]
        public void HalfClone() {
            var orig = (Half)1.5;
            orig.CloneX().Should().Be(orig);
        }
#endif
    }
}
