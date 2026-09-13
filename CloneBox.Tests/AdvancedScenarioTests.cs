using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class AdvancedScenarioTests {

        public class BaseNode {
            public int Id { get; set; }
        }

        public class Leaf : BaseNode {
            public string Name { get; set; }
        }

        public class WithStatic {
            public static int StaticValue = 10;
            public static string StaticName { get; set; } = "shared";
            public int InstanceValue { get; set; }
        }

        public class Computed {
            public int A { get; set; }
            public int B { get; set; }
            public int Sum => A + B;
        }

        public class StringIndexed {
            public Dictionary<string, int> Values { get; set; } = new Dictionary<string, int>();
            public int this[string key] {
                get => Values[key];
                set => Values[key] = value;
            }
        }

        public class CustomDynamic : DynamicObject {
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

            public override bool TrySetMember(SetMemberBinder binder, object value) {
                _values[binder.Name] = value;
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result) {
                return _values.TryGetValue(binder.Name, out result);
            }

            public override IEnumerable<string> GetDynamicMemberNames() => _values.Keys;

            public IDictionary<string, object> Data => _values;
        }

        [Fact]
        public void PolymorphicListKeepsRuntimeTypes() {
            var orig = new List<BaseNode> {
                new BaseNode { Id = 1 },
                new Leaf { Id = 2, Name = "leaf" }
            };
            var clone = orig.CloneX();
            clone[0].Should().BeOfType<BaseNode>();
            clone[1].Should().BeOfType<Leaf>();
            ((Leaf)clone[1]).Name.Should().Be("leaf");
            clone[1].Should().NotBeSameAs(orig[1]);
        }

        [Fact]
        public void ObjectArrayMixedTypes() {
            var orig = new object[] { 1, "a", new Leaf { Id = 3, Name = "x" }, null };
            var clone = orig.CloneX();
            clone[0].Should().Be(1);
            clone[1].Should().Be("a");
            clone[2].Should().BeOfType<Leaf>();
            ((Leaf)clone[2]).Name.Should().Be("x");
            clone[2].Should().NotBeSameAs(orig[2]);
            clone[3].Should().BeNull();
        }

        [Fact]
        public void StaticMembersAreNotCopiedOntoClone() {
            WithStatic.StaticValue = 10;
            WithStatic.StaticName = "shared";
            var orig = new WithStatic { InstanceValue = 5 };
            var clone = orig.CloneX();
            clone.InstanceValue.Should().Be(5);
            WithStatic.StaticValue = 99;
            WithStatic.StaticName = "changed";
            WithStatic.StaticValue.Should().Be(99);
            clone.InstanceValue.Should().Be(5);
        }

        [Fact]
        public void ComputedPropertyFollowsCopiedInputs() {
            var orig = new Computed { A = 2, B = 3 };
            var clone = orig.CloneX();
            clone.Sum.Should().Be(5);
            clone.A = 10;
            clone.Sum.Should().Be(13);
            orig.Sum.Should().Be(5);
        }

        [Fact]
        public void StringIndexerValuesAreCopiedViaBackingStore() {
            var orig = new StringIndexed();
            orig["a"] = 1;
            orig["b"] = 2;
            var clone = orig.CloneX();
            clone.Values.Should().NotBeSameAs(orig.Values);
            clone["a"].Should().Be(1);
            clone["b"].Should().Be(2);
        }

        [Fact]
        public void CustomDynamicObject() {
            dynamic orig = new CustomDynamic();
            orig.Id = 1;
            orig.Name = "dyn";
            var clone = CloneXExtensions.CloneX((CustomDynamic)orig);
            clone.Should().NotBeSameAs(orig);
            clone.Data["Id"].Should().Be(1);
            clone.Data["Name"].Should().Be("dyn");
        }

        [Fact]
        public void EmptyExpandoObject() {
            var orig = new ExpandoObject();
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            ((IDictionary<string, object>)clone).Should().BeEmpty();
        }

        [Fact]
        public void DictionaryStringObject() {
            var orig = new Dictionary<string, object> {
                { "id", 1 },
                { "child", new Leaf { Id = 2, Name = "c" } }
            };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone["id"].Should().Be(1);
            clone["child"].Should().BeOfType<Leaf>();
            clone["child"].Should().NotBeSameAs(orig["child"]);
        }

        [Fact]
        public void WeakReferenceClone() {
            var target = new Leaf { Id = 1, Name = "w" };
            var orig = new WeakReference(target);
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Should().NotBeSameAs(orig);
        }

        [Fact]
        public void LazyValueIsCloned() {
            var orig = new Lazy<Leaf>(() => new Leaf { Id = 1, Name = "lazy" });
            var created = orig.Value;
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Value.Name.Should().Be("lazy");
            clone.Value.Should().NotBeSameAs(created);
        }

        [Fact]
        public void ParallelClonesOfSameType() {
            var source = new Leaf { Id = 1, Name = "p" };
            var results = new Leaf[50];
            Parallel.For(0, results.Length, i => results[i] = source.CloneX());
            results.Should().OnlyContain(x => x != null && x.Name == "p" && !ReferenceEquals(x, source));
            results.Distinct().Should().HaveCount(results.Length);
        }

        [Fact]
        public void VeryDeepNesting() {
            var start = new Linked { Id = 0 };
            var cursor = start;
            for (int i = 1; i <= 200; i++) {
                cursor.Next = new Linked { Id = i };
                cursor = cursor.Next;
            }
            var clone = start.CloneX();
            clone.Should().NotBeSameAs(start);
            var walk = clone;
            for (int i = 0; i <= 200; i++) {
                walk.Id.Should().Be(i);
                walk = walk.Next;
            }
            walk.Should().BeNull();
        }

        public class Linked {
            public int Id { get; set; }
            public Linked Next { get; set; }
        }

        [Fact]
        public void CloneXIsDeepNotShallow() {
            var orig = new ClassRecordHolder { Child = new Leaf { Id = 1, Name = "n" } };
            var clone = orig.CloneX();
            clone.Child.Name = "changed";
            orig.Child.Name.Should().Be("n");
        }

        public class ClassRecordHolder {
            public Leaf Child { get; set; }
        }

    }
}
