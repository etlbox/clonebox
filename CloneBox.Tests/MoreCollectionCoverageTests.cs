using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xunit;

#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace CloneBox.Tests {
    public class MoreCollectionCoverageTests {

        public class Item {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class IdComparer : IEqualityComparer<Item> {
            public bool Equals(Item x, Item y) => x?.Id == y?.Id;
            public int GetHashCode(Item obj) => obj?.Id ?? 0;
        }

        [Fact]
        public void EmptyHashSetQueueAndStack() {
            new HashSet<int>().CloneX().Should().BeEmpty();
            new Queue<int>().CloneX().Should().BeEmpty();
            new Stack<int>().CloneX().Should().BeEmpty();
        }

        [Fact]
        public void HashSetAllowsNullAndKeepsIt() {
            var orig = new HashSet<string> { "a", null };
            var clone = orig.CloneX();
            clone.Should().HaveCount(2);
            clone.Should().Contain((string)null);
            clone.Should().Contain("a");
        }

        [Fact]
        public void HashSetKeepsCustomComparer() {
            var orig = new HashSet<Item>(new IdComparer()) { new Item { Id = 1, Name = "A" } };
            var clone = orig.CloneX();
            clone.Contains(new Item { Id = 1, Name = "other" }).Should().BeTrue();
        }

        [Fact]
        public void QueueOfObjectsIsFifoAndDeep() {
            var first = new Item { Id = 1, Name = "A" };
            var orig = new Queue<Item>();
            orig.Enqueue(first);
            orig.Enqueue(new Item { Id = 2, Name = "B" });
            var clone = orig.CloneX();
            clone.Dequeue().Should().NotBeSameAs(first);
            clone.Dequeue().Name.Should().Be("B");
            orig.Peek().Should().BeSameAs(first);
        }

        [Fact]
        public void StackOfObjectsIsLifoAndDeep() {
            var top = new Item { Id = 2, Name = "B" };
            var orig = new Stack<Item>();
            orig.Push(new Item { Id = 1, Name = "A" });
            orig.Push(top);
            var clone = orig.CloneX();
            clone.Pop().Should().NotBeSameAs(top);
            clone.Pop().Name.Should().Be("A");
            orig.Peek().Should().BeSameAs(top);
        }

        [Fact]
        public void SortedSetKeepsComparer() {
            var orig = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var clone = orig.CloneX();
            clone.Contains("a").Should().BeTrue();
        }

        [Fact]
        public void CollectionOfT() {
            var orig = new Collection<Item> { new Item { Id = 1, Name = "A" } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone[0].Should().NotBeSameAs(orig[0]);
            clone[0].Name.Should().Be("A");
        }

        [Fact]
        public void BindingListOfObjects() {
            var orig = new BindingList<Item> { new Item { Id = 1, Name = "A" } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(1);
            clone[0].Name.Should().Be("A");
            clone[0].Should().NotBeSameAs(orig[0]);
        }

        [Fact]
        public void ReadOnlyDictionary() {
            var orig = new ReadOnlyDictionary<string, Item>(
                new Dictionary<string, Item> { { "a", new Item { Id = 1, Name = "A" } } });
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().HaveCount(1);
            clone["a"].Name.Should().Be("A");
            clone["a"].Should().NotBeSameAs(orig["a"]);
        }

        [Fact]
        public void OrderedDictionaryKeepsInsertionOrder() {
            var orig = new OrderedDictionary { { "c", 3 }, { "a", 1 }, { "b", 2 } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Keys.Cast<object>().Should().Equal("c", "a", "b");
            clone["a"].Should().Be(1);
        }

        [Fact]
        public void NameValueCollectionKeepsDuplicateKeys() {
            var orig = new NameValueCollection { { "a", "1" }, { "a", "2" }, { "b", "3" } };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.GetValues("a").Should().Equal("1", "2");
            clone["b"].Should().Be("3");
        }

        [Fact]
        public void BitArrayCopiesBitsIndependently() {
            var orig = new BitArray(new[] { true, false, true });
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Length.Should().Be(3);
            clone[0].Should().BeTrue();
            clone[1].Should().BeFalse();
            orig[0] = false;
            clone[0].Should().BeTrue();
        }

        [Fact]
        public void ConcurrentQueueStackAndBag() {
            var queue = new ConcurrentQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            var queueClone = queue.CloneX();
            queueClone.Should().NotBeSameAs(queue);
            queueClone.Should().BeEquivalentTo(new[] { 1, 2 });

            var stack = new ConcurrentStack<string>();
            stack.Push("a");
            stack.Push("b");
            var stackClone = stack.CloneX();
            stackClone.Should().BeEquivalentTo(new[] { "b", "a" });

            var bag = new ConcurrentBag<int> { 1, 2, 3 };
            var bagClone = bag.CloneX();
            bagClone.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void NonGenericQueueAndStack() {
            var queue = new Queue();
            queue.Enqueue(1);
            queue.Enqueue(new Item { Id = 2, Name = "B" });
            var queueClone = queue.CloneX();
            queueClone.Dequeue().Should().Be(1);
            ((Item)queueClone.Dequeue()).Name.Should().Be("B");

            var stack = new Stack();
            stack.Push("x");
            var stackClone = stack.CloneX();
            stackClone.Pop().Should().Be("x");
        }

        [Fact]
        public void NestedListOfLists() {
            var shared = new List<int> { 1, 2 };
            var orig = new List<List<int>> { shared, shared, new List<int> { 3 } };
            var clone = orig.CloneX();
            clone[0].Should().BeSameAs(clone[1]);
            clone[0].Should().NotBeSameAs(shared);
            clone[0].Should().Equal(1, 2);
            clone[2].Should().Equal(3);
            shared[0] = 9;
            clone[0][0].Should().Be(1);
        }

        [Fact]
        public void DictionaryOfLists() {
            var orig = new Dictionary<string, List<Item>> {
                { "a", new List<Item> { new Item { Id = 1, Name = "A" } } }
            };
            var clone = orig.CloneX();
            clone["a"].Should().NotBeSameAs(orig["a"]);
            clone["a"][0].Should().NotBeSameAs(orig["a"][0]);
            clone["a"][0].Name.Should().Be("A");
        }

        [Fact]
        public void ListOfArrays() {
            var orig = new List<int[]> { new[] { 1, 2 }, new[] { 3 } };
            var clone = orig.CloneX();
            clone[0].Should().NotBeSameAs(orig[0]);
            clone[0].Should().Equal(1, 2);
            orig[0][0] = 9;
            clone[0][0].Should().Be(1);
        }

        [Fact]
        public void DictionaryWithEnumKeys() {
            var orig = new Dictionary<DayOfWeek, Item> {
                { DayOfWeek.Monday, new Item { Id = 1, Name = "M" } }
            };
            var clone = orig.CloneX();
            clone[DayOfWeek.Monday].Name.Should().Be("M");
            clone[DayOfWeek.Monday].Should().NotBeSameAs(orig[DayOfWeek.Monday]);
        }

        public class InterfaceHolder {
            public IList<int> Numbers { get; set; }
            public IReadOnlyDictionary<string, Item> Map { get; set; }
            public IDictionary Bag { get; set; }
        }

        [Fact]
        public void InterfaceTypedListAndDictionaryUseRuntimeType() {
            var orig = new InterfaceHolder {
                Numbers = new List<int> { 1, 2 },
                Map = new Dictionary<string, Item> { { "a", new Item { Id = 1, Name = "A" } } },
                Bag = new Hashtable { { "k", 5 } }
            };
            var clone = orig.CloneX();
            clone.Numbers.Should().Equal(1, 2);
            clone.Numbers.Should().NotBeSameAs(orig.Numbers);
            clone.Map["a"].Name.Should().Be("A");
            clone.Map["a"].Should().NotBeSameAs(orig.Map["a"]);
            clone.Bag["k"].Should().Be(5);
        }

        [Fact]
        public void KeyedCollectionClonesItemsAndCustomState() {
            var orig = new ItemKeyedCollection { new Item { Id = 1, Name = "A" } };
            orig.Tag = "keep";
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Tag.Should().Be("keep");
            clone[1].Name.Should().Be("A");
            clone[1].Should().NotBeSameAs(orig[1]);
        }

        public class ItemKeyedCollection : KeyedCollection<int, Item> {
            public string Tag { get; set; }
            protected override int GetKeyForItem(Item item) => item.Id;
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void FrozenSetAndDictionary() {
            var set = new[] { 1, 2, 3 }.ToFrozenSet();
            var setClone = set.CloneX();
            setClone.Should().BeEquivalentTo(set);

            var dict = new Dictionary<string, int> { { "a", 1 } }.ToFrozenDictionary();
            var dictClone = dict.CloneX();
            dictClone["a"].Should().Be(1);
        }

        [Fact]
        public void PriorityQueueKeepsOrder() {
            var orig = new PriorityQueue<string, int>();
            orig.Enqueue("b", 2);
            orig.Enqueue("a", 1);
            var clone = orig.CloneX();
            clone.Dequeue().Should().Be("a");
            clone.Dequeue().Should().Be("b");
            orig.Peek().Should().Be("a");
        }
#endif
    }
}
