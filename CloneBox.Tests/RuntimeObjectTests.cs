using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class RuntimeObjectTests {

        public class Item {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void AnonymousTypeDeepClone() {
            var orig = new { Id = 1, Name = "A", Child = new Item { Id = 2, Name = "B" } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(1);
            clone.Name.Should().Be("A");
            clone.Child.Name.Should().Be("B");
            clone.Child.Should().NotBeSameAs(orig.Child);
        }

        [Fact]
        public void KeyValuePairWithObjectValue() {
            var orig = new KeyValuePair<string, Item>("k", new Item { Id = 1, Name = "A" });
            var clone = orig.CloneX();
            clone.Key.Should().Be("k");
            clone.Value.Name.Should().Be("A");
            clone.Value.Should().NotBeSameAs(orig.Value);
        }

        [Fact]
        public void BoxedValueInsideObjectProperty() {
            var orig = new Box { Value = 42, Label = "n" };
            var clone = orig.CloneX();
            clone.Value.Should().Be(42);
            clone.Value.Should().BeOfType<int>();
            clone.Label.Should().Be("n");
        }

        public class Box {
            public object Value { get; set; }
            public string Label { get; set; }
        }

        [Fact]
        public void NullableProperties() {
            var orig = new NullableHolder {
                Number = 5,
                When = new DateTime(2020, 1, 2),
                Missing = null
            };
            var clone = orig.CloneX();
            clone.Number.Should().Be(5);
            clone.When.Should().Be(new DateTime(2020, 1, 2));
            clone.Missing.Should().BeNull();
        }

        public class NullableHolder {
            public int? Number { get; set; }
            public DateTime? When { get; set; }
            public Guid? Missing { get; set; }
        }

        [Fact]
        public void TypeAsProperty() {
            var orig = new TypeHolder { Type = typeof(string), Name = "s" };
            var clone = orig.CloneX();
            clone.Name.Should().Be("s");
            clone.Type.Should().BeSameAs(typeof(string));
        }

        public class TypeHolder {
            public Type Type { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void IpAddressIsUsableAfterClone() {
            var orig = IPAddress.Parse("127.0.0.1");
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.ToString().Should().Be("127.0.0.1");
        }

        [Fact]
        public void MemoryStreamCopiesBuffer() {
            var orig = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            Encoding.UTF8.GetString(clone.ToArray()).Should().Be("hello");
            orig.SetLength(0);
            Encoding.UTF8.GetString(clone.ToArray()).Should().Be("hello");
        }

        [Fact]
        public void EncodingAndRegexStayUsable() {
            var encodingClone = Encoding.UTF8.CloneX();
            encodingClone.GetString(encodingClone.GetBytes("ä")).Should().Be("ä");

            var regex = new Regex("^a+$", RegexOptions.IgnoreCase);
            var regexClone = regex.CloneX();
            regexClone.IsMatch("AAA").Should().BeTrue();
        }

        [Fact]
        public void ExceptionKeepsInnerException() {
            var orig = new InvalidOperationException("outer", new ArgumentException("inner"));
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Message.Should().Be("outer");
            clone.InnerException.Should().NotBeNull();
            clone.InnerException.Message.Should().Be("inner");
            clone.InnerException.Should().NotBeSameAs(orig.InnerException);
        }

        [Fact]
        public async Task CompletedTaskCloneStaysCompleted() {
            var orig = Task.FromResult(12);
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.IsCompleted.Should().BeTrue();
            (await clone).Should().Be(12);
        }

        [Fact]
        public void CancellationTokenClone() {
            using var cts = new CancellationTokenSource();
            var orig = cts.Token;
            var clone = orig.CloneX();
            clone.IsCancellationRequested.Should().BeFalse();
            cts.Cancel();
            orig.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public void ArraySegmentClone() {
            var buffer = new[] { 1, 2, 3, 4 };
            var orig = new ArraySegment<int>(buffer, 1, 2);
            var clone = orig.CloneX();
            clone.Should().Equal(2, 3);
        }

        [Fact]
        public void WeakReferenceOfT() {
            var target = new Item { Id = 1, Name = "w" };
            var orig = new WeakReference<Item>(target);
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.TryGetTarget(out var clonedTarget).Should().BeTrue();
            clonedTarget.Name.Should().Be("w");
        }

        [Fact]
        public void LazyWithoutValueDoesNotForceCreationDuringClone() {
            var created = 0;
            var orig = new Lazy<Item>(() => {
                created++;
                return new Item { Id = 1, Name = "lazy" };
            });
            var clone = orig.CloneX();
            orig.IsValueCreated.Should().BeFalse();
            clone.Should().NotBeNull();
        }

        [Fact]
        public void GetterOnlyClassPropertyUsesBackingStore() {
            var orig = new GetterOnly("hidden");
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Name.Should().Be("hidden");
        }

        public class GetterOnly {
            public GetterOnly(string name) {
                Name = name;
            }
            public string Name { get; }
        }

    }
}
