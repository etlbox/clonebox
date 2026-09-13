using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace CloneBox.Tests {
    public class CollectionCoverageTests {

        public class Item {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void HashSetOfInts() {
            var orig = new HashSet<int> { 1, 2, 3, 3 };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
        }

        [Fact]
        public void HashSetOfObjects() {
            var shared = new Item { Id = 1, Name = "A" };
            var orig = new HashSet<Item> { shared, new Item { Id = 2, Name = "B" }, shared };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(2);
            clone.Should().NotContain(shared);
        }

        [Fact]
        public void HashSetKeepsComparer() {
            var orig = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var clone = orig.CloneX();
            clone.Contains("a").Should().BeTrue();
        }

        [Fact]
        public void QueueOfInts() {
            var orig = new Queue<int>();
            orig.Enqueue(1);
            orig.Enqueue(2);
            orig.Enqueue(3);
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
            clone.Dequeue().Should().Be(1);
        }

        [Fact]
        public void StackOfInts() {
            var orig = new Stack<int>();
            orig.Push(1);
            orig.Push(2);
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Pop().Should().Be(2);
            orig.Peek().Should().Be(2);
        }

        [Fact]
        public void LinkedListOfObjects() {
            var orig = new LinkedList<Item>();
            orig.AddLast(new Item { Id = 1, Name = "A" });
            orig.AddLast(new Item { Id = 2, Name = "B" });
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.First.Value.Should().NotBeSameAs(orig.First.Value);
            clone.First.Value.Name.Should().Be("A");
        }

        [Fact]
        public void SortedDictionary() {
            var orig = new SortedDictionary<int, string> { { 3, "c" }, { 1, "a" } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
            clone.Keys.Should().BeInAscendingOrder();
        }

        [Fact]
        public void SortedSet() {
            var orig = new SortedSet<int> { 5, 1, 3 };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
            clone.Min.Should().Be(1);
        }

        [Fact]
        public void ObservableCollection() {
            var orig = new ObservableCollection<Item> {
                new Item { Id = 1, Name = "A" },
                new Item { Id = 2, Name = "B" }
            };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(2);
            clone[0].Should().NotBeSameAs(orig[0]);
            clone[0].Name.Should().Be("A");
        }

        [Fact]
        public void ConcurrentDictionary() {
            var orig = new ConcurrentDictionary<string, Item>();
            orig["a"] = new Item { Id = 1, Name = "A" };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone["a"].Name.Should().Be("A");
            clone["a"].Should().NotBeSameAs(orig["a"]);
        }

        [Fact]
        public void ArrayListAndHashtable() {
            var list = new ArrayList { 1, "x", new Item { Id = 2, Name = "B" } };
            var listClone = list.CloneX();
            listClone.Should().NotBeSameAs(list);
            listClone.Count.Should().Be(3);
            ((Item)listClone[2]).Name.Should().Be("B");
            ((Item)listClone[2]).Should().NotBeSameAs(list[2]);

            var table = new Hashtable { { "k", new Item { Id = 3, Name = "C" } } };
            var tableClone = table.CloneX();
            tableClone.Should().NotBeSameAs(table);
            ((Item)tableClone["k"]).Name.Should().Be("C");
        }

        [Fact]
        public void ReadOnlyCollection() {
            var orig = new List<int> { 1, 2, 3, 4, 5 }.AsReadOnly();
            var clone = orig.CloneX();
            clone.Should().NotBeNull();
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(5);
            clone.Should().BeEquivalentTo(orig);
        }

        [Fact]
        public void EmptyListDictionaryAndArray() {
            new List<int>().CloneX().Should().BeEmpty();
            new Dictionary<string, int>().CloneX().Should().BeEmpty();
            new int[0].CloneX().Should().BeEmpty();
            new Item[0].CloneX().Should().BeEmpty();
        }

        [Fact]
        public void JaggedIntArray() {
            var orig = new[] { new[] { 1, 2 }, new[] { 3 } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone[0].Should().NotBeSameAs(orig[0]);
            clone[0][1].Should().Be(2);
            clone[1][0].Should().Be(3);
            orig[0][0] = 9;
            clone[0][0].Should().Be(1);
        }

        [Fact]
        public void JaggedObjectArraySharesInnerReferences() {
            var item = new Item { Id = 1, Name = "A" };
            var orig = new[] { new[] { item, item }, new[] { item } };
            var clone = orig.CloneX();
            clone[0][0].Should().BeSameAs(clone[0][1]);
            clone[0][0].Should().BeSameAs(clone[1][0]);
            clone[0][0].Should().NotBeSameAs(item);
        }

        [Fact]
        public void DictionaryDoesNotCloneKeys() {
            var key = new Item { Id = 1, Name = "key" };
            var orig = new Dictionary<Item, string> { { key, "v" } };
            var clone = orig.CloneX();
            clone.Should().ContainKey(key);
            foreach (var k in clone.Keys)
                k.Should().BeSameAs(key);
        }

        [Fact]
        public void DictionaryComparerIsPreserved() {
            var orig = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "A", 1 } };
            var clone = orig.CloneX();
            clone.ContainsKey("a").Should().BeTrue();
        }

        public class Holder {
            public IReadOnlyList<int> Numbers { get; set; }
            public ISet<string> Names { get; set; }
        }

        [Fact]
        public void InterfaceTypedCollectionsUseRuntimeType() {
            var orig = new Holder {
                Numbers = new List<int> { 1, 2 },
                Names = new HashSet<string> { "a", "b" }
            };
            var clone = orig.CloneX();
            clone.Numbers.Should().NotBeSameAs(orig.Numbers);
            clone.Numbers.Should().BeEquivalentTo(orig.Numbers);
            clone.Names.Should().NotBeSameAs(orig.Names);
            clone.Names.Should().BeEquivalentTo(orig.Names);
        }
    }
}
