using FluentAssertions;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace CloneBox.Tests {
    public class GraphAndMemberTests {

        public class Node {
            public int Id { get; set; }
            public HashSet<Node> Set { get; set; }
            public List<Node> Others { get; set; }
        }

        public class Item {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void CycleThroughHashSet() {
            var root = new Node { Id = 1, Set = new HashSet<Node>() };
            root.Set.Add(root);
            var clone = root.CloneX();
            clone.Should().NotBeSameAs(root);
            clone.Set.Should().NotBeSameAs(root.Set);
            clone.Set.Should().HaveCount(1);
            clone.Set.Should().Contain(clone);
        }

        [Fact]
        public void MutualLists() {
            var a = new List<object>();
            var b = new List<object>();
            a.Add(b);
            b.Add(a);
            var clone = a.CloneX();
            clone.Should().NotBeSameAs(a);
            clone[0].Should().BeOfType<List<object>>();
            ((List<object>)clone[0])[0].Should().BeSameAs(clone);
        }

        [Fact]
        public void SelfReferencingArrayAsRoot() {
            var orig = new object[1];
            orig[0] = orig;
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone[0].Should().BeSameAs(clone);
        }

        [Fact]
        public void ExpandoNestedInPoco() {
            dynamic child = new ExpandoObject();
            child.Name = "dyn";
            var orig = new ExpandoHolder { Child = child };
            var clone = orig.CloneX();
            clone.Child.Should().NotBeSameAs(orig.Child);
            ((IDictionary<string, object>)clone.Child)["Name"].Should().Be("dyn");
        }

        public class ExpandoHolder {
            public ExpandoObject Child { get; set; }
        }

        [Fact]
        public void PocoNestedInExpando() {
            dynamic orig = new ExpandoObject();
            orig.Child = new Item { Id = 1, Name = "A" };
            var clone = CloneXExtensions.CloneX((ExpandoObject)orig);
            var child = ((IDictionary<string, object>)clone)["Child"] as Item;
            child.Should().NotBeNull();
            child.Name.Should().Be("A");
            child.Should().NotBeSameAs(((IDictionary<string, object>)orig)["Child"]);
        }

        [Fact]
        public void ObjectPropertyHoldingList() {
            var orig = new ObjectHolder {
                Value = new List<Item> { new Item { Id = 1, Name = "A" } }
            };
            var clone = orig.CloneX();
            var list = (List<Item>)clone.Value;
            list.Should().NotBeSameAs((List<Item>)orig.Value);
            list[0].Name.Should().Be("A");
        }

        public class ObjectHolder {
            public object Value { get; set; }
        }

        [Fact]
        public void ExplicitInterfaceMembersAreCopied() {
            var orig = new ExplicitName { Id = 4 };
            ((IHasName)orig).Name = "hidden";
            var clone = orig.CloneX();
            clone.Id.Should().Be(4);
            ((IHasName)clone).Name.Should().Be("hidden");
            clone.Should().NotBeSameAs(orig);
        }

        public interface IHasName {
            string Name { get; set; }
        }

        public class ExplicitName : IHasName {
            public int Id { get; set; }
            string IHasName.Name { get; set; }
        }

        [Fact]
        public void GenericDerivedClassKeepsBaseAndOwnMembers() {
            var orig = new IntBox { Value = 9, Label = "n" };
            var clone = orig.CloneX();
            clone.Value.Should().Be(9);
            clone.Label.Should().Be("n");
            clone.Should().NotBeSameAs(orig);
        }

        public class Box<T> {
            public T Value { get; set; }
        }

        public class IntBox : Box<int> {
            public string Label { get; set; }
        }

        [Fact]
        public void MultiParameterIndexerDoesNotPreventOtherMembers() {
            var orig = new Grid();
            orig[1, 2] = 9;
            orig.Name = "g";
            var clone = orig.CloneX();
            clone.Name.Should().Be("g");
            clone.Cells.Should().NotBeSameAs(orig.Cells);
            clone[1, 2].Should().Be(9);
        }

        public class Grid {
            public Dictionary<string, int> Cells { get; set; } = new Dictionary<string, int>();
            public string Name { get; set; }
            public int this[int x, int y] {
                get => Cells[x + "," + y];
                set => Cells[x + "," + y] = value;
            }
        }

        [Fact]
        public void ActionPropertyKeepsDelegateUsable() {
            var hits = 0;
            var orig = new HasAction { Run = () => hits++ };
            var clone = orig.CloneX();
            clone.Run.Should().NotBeNull();
            clone.Run();
            hits.Should().BeGreaterThan(0);
        }

        public class HasAction {
            public System.Action Run { get; set; }
        }

        [Fact]
        public void InheritedDoNotClonePropertyIsSkipped() {
            var orig = new DerivedMarked { Keep = "A", Skip = "B", Extra = "C" };
            var clone = orig.CloneX();
            clone.Keep.Should().Be("A");
            clone.Skip.Should().BeNull();
            clone.Extra.Should().Be("C");
        }

        public class BaseMarked {
            public string Keep { get; set; }
            [DoNotClone]
            public string Skip { get; set; }
        }

        public class DerivedMarked : BaseMarked {
            public string Extra { get; set; }
        }
    }
}
