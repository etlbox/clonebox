using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class EnumerablesTests {


        [Fact]
        public void IntList() {
            var orig = new List<int> { 1, 2, 3, 0, 1, 3 };
            var cloned = orig.CloneX();
            cloned.Should().NotBeSameAs(orig);
            cloned.Should().HaveCount(6);
            cloned.Should().BeEquivalentTo(orig);
            
        }

        [Fact]
        public void NullableIntList() {
            var orig = new List<int?> { 1, 2, null, 0, null, 3 };
            var cloned = orig.CloneX();
            cloned.Should().NotBeSameAs(orig);
            cloned.Should().HaveCount(6);
            cloned.Should().BeEquivalentTo(orig);
            
        }

        [Fact]
        public void Dictionary() {
            var orig = new Dictionary<string, decimal?>();
            orig["a"] = 1;
            orig["b"] = null;
            orig["c"] = 3;
            var cloned = orig.CloneX();
            cloned.Should().NotBeSameAs(orig);
            cloned.Should().HaveCount(3);
            cloned.Should().BeEquivalentTo(orig);
            
        }

        public class BasicObject {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class CollectionObject {
            public ICollection<int> IntCollection { get; set; }
            public ICollection<BasicObject> ObjectCollection { get; set; }

          
        }

        [Fact]
        public void ICollections() {
            CollectionObject orig = new CollectionObject() {
                IntCollection = new List<int>() { 1, 2, 3 },
                ObjectCollection = new List<BasicObject>() {
                     new BasicObject() { Id = 1, Name = "One" },
                     new BasicObject() { Id = 2, Name = "Two" },
                     new BasicObject() { Id = 3, Name = "Three" }
                }
            };

            var clone = orig.CloneX();
            Assert.True(clone != orig);
            Assert.True(orig.IntCollection != clone.IntCollection);
            Assert.True(orig.ObjectCollection != clone.ObjectCollection);

            clone.IntCollection.Should().HaveCount(3);
            clone.ObjectCollection.Should().HaveCount(3);
        }

        public class DictionaryObject { 
            public IDictionary<int, BasicObject> Collection { get; set; } = new Dictionary<int, BasicObject>();
        }

        [Fact]
        public void CloneDictionaryObject() {
            var orig = new DictionaryObject {
                Collection = {
                    { 1, new BasicObject() { Id = 1, Name = "Test1" } },
                    { 2, new BasicObject() { Id = 2, Name= "Test2"} }
                }
            };
            var clone = orig.CloneX();

            orig.Should().NotBeSameAs(clone);
            clone.Collection.Should().HaveCount(2);
            clone.Collection[1].Id.Should().Be(1);
            clone.Collection[1].Name.Should().Be("Test1");
        }

        public class CustomCollectionObject<T> : Collection<T> {
            public int CustomId { get; set; }
            public string CustomName { get; set; }
            public CustomCollectionObject(int customId, string customName) {
                CustomId = customId;
                CustomName = customName;
            }
        }

        [Fact]
        public void CustomCollection() {
            var orig = new CustomCollectionObject<BasicObject>(100, "test")
            {
                new BasicObject() { Id = 1, Name = "Test1" },
                new BasicObject() { Id = 2, Name = "Test2" },
            };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
            clone.Count.Should().Be(2);
        }

        [Fact]
        public void ListWithObject() {
            List<BasicObject> orig = new List<BasicObject>() {
                new BasicObject() { Id = 1, Name = "One" },
                new BasicObject() { Id = 2, Name = "Two" },
                new BasicObject() { Id = 3, Name = "Three" }
            };              
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
        }

       

        [Fact]
        public void SortedList() {
            SortedList<int, string> orig = new SortedList<int, string> {
                { 3, "Three" },
                { 1, "One" },
                { 2, "Two" },
                { 4, null },
                { 10, "Ten" },
                { 5, "Five" }
            };
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            clone.Should().BeEquivalentTo(orig);
        }

        [Fact]
        public void SortedListValues() {
            SortedList<int, string> orig = new SortedList<int, string> {
                { 3, "Three" },
                { 1, "One" },
                { 2, "Two" },
                { 4, null },
                { 10, "Ten" },
                { 5, "Five" }
            };
            var clone = orig.Values.CloneX();
            //We can't copy, so we just keep the reference!
            clone.Should().BeSameAs(orig.Values);
            clone.Should().BeEquivalentTo(orig.Values);
        }

        [Fact]
        public void ListAsIEnumerable() {
            List<BasicObject> orig = new List<BasicObject>() {
                new BasicObject() { Id = 1, Name = "One" },
                new BasicObject() { Id = 2, Name = "Two" },
                new BasicObject() { Id = 3, Name = "Three" }
            };
            var enumberable = orig.Select(rec => rec.Name);
            var clone = enumberable.CloneX();
            //We can't copy, so we just keep the reference!
            clone.Should().BeSameAs(enumberable);
            clone.Should().BeEquivalentTo(enumberable);
        }




    }
}
