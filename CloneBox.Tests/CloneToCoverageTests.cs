using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace CloneBox.Tests {
    public class CloneToCoverageTests {

        public class Person {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Extra { get; set; }
        }

        public class SourcePerson {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class TargetPerson : SourcePerson {
            public string Extra { get; set; }
        }

        [Fact]
        public void ExpandoToExpando() {
            dynamic source = new ExpandoObject();
            source.Id = 1;
            source.Name = "A";
            dynamic target = new ExpandoObject();
            CloneXExtensions.CloneXTo(source, (ExpandoObject)target);
            Assert.Equal(1, target.Id);
            Assert.Equal("A", target.Name);
            ((object)target).Should().NotBeSameAs((object)source);
        }

        [Fact]
        public void ListToEmptyList() {
            var source = new List<int> { 1, 2, 3 };
            var target = new List<int>();
            source.CloneXTo(target);
            target.Should().Equal(1, 2, 3);
        }

        [Fact]
        public void ListToNonEmptyListAppends() {
            var source = new List<int> { 1, 2 };
            var target = new List<int> { 9 };
            source.CloneXTo(target);
            target.Should().Equal(9, 1, 2);
        }

        [Fact]
        public void DictionaryToEmptyDictionary() {
            var source = new Dictionary<string, int> { { "a", 1 } };
            var target = new Dictionary<string, int>();
            source.CloneXTo(target);
            target["a"].Should().Be(1);
        }

        [Fact]
        public void DictionaryToNonEmptyDictionaryWithNewKeys() {
            var source = new Dictionary<string, int> { { "b", 2 } };
            var target = new Dictionary<string, int> { { "a", 1 } };
            source.CloneXTo(target);
            target.Should().HaveCount(2);
            target["a"].Should().Be(1);
            target["b"].Should().Be(2);
        }

        [Fact]
        public void DictionaryToNonEmptyDictionaryWithDuplicateKeys() {
            var source = new Dictionary<string, int> { { "a", 2 } };
            var target = new Dictionary<string, int> { { "a", 1 } };
            Action act = () => source.CloneXTo(target);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ExtraTargetPropertiesArePreserved() {
            var source = new SourcePerson { Id = 1, Name = "A" };
            var target = new TargetPerson { Extra = "keep" };
            source.CloneXTo(target);
            target.Id.Should().Be(1);
            target.Name.Should().Be("A");
            target.Extra.Should().Be("keep");
        }

        [Fact]
        public void CloneXToHonorsSettings() {
            var source = new Person { Id = 1, Name = "A", Extra = "E" };
            var target = new Person { Extra = "keep" };
            source.CloneXTo(target, new CloneSettings {
                DoNotCloneProperty = p => p.Name == "Name"
            });
            target.Id.Should().Be(1);
            target.Name.Should().BeNull();
            target.Extra.Should().Be("E");
        }

        [Fact]
        public void CloneXToListOfObjectsKeepsGraph() {
            var shared = new Person { Id = 1, Name = "A" };
            var source = new List<Person> { shared, shared };
            var target = new List<Person>();
            source.CloneXTo(target);
            target.Should().HaveCount(2);
            target[0].Should().BeSameAs(target[1]);
            target[0].Should().NotBeSameAs(shared);
        }

        [Fact]
        public void CloneXToFromListToArrayCopiesOverlappingItems() {
            var source = new List<int> { 1, 2, 3 };
            var target = new int[2];
            source.CloneXTo(target);
            target[0].Should().Be(1);
            target[1].Should().Be(2);
        }

        [Fact]
        public void CloneXToWithoutDestinationKeepsRuntimeType() {
            var source = new TargetPerson { Id = 1, Name = "A", Extra = "E" };
            var clone = source.CloneXTo<TargetPerson, SourcePerson>();
            clone.Should().BeOfType<TargetPerson>();
            clone.Id.Should().Be(1);
            clone.Name.Should().Be("A");
            ((TargetPerson)clone).Extra.Should().Be("E");
        }

        [Fact]
        public void CloneXToNullDestinationReturnsNull() {
            var source = new Person { Id = 1, Name = "A" };
            source.CloneXTo<Person, Person>((Person)null).Should().BeNull();
        }

        [Fact]
        public void CloneXToSameInstanceKeepsIdentityAndValues() {
            var obj = new Person { Id = 1, Name = "A" };
            var result = obj.CloneXTo(obj);
            result.Should().BeSameAs(obj);
            result.Id.Should().Be(1);
            result.Name.Should().Be("A");
        }

        [Fact]
        public void ExpandoToPoco() {
            dynamic source = new ExpandoObject();
            source.Id = 7;
            source.Name = "dyn";
            var target = new Person();
            CloneXExtensions.CloneXTo((ExpandoObject)source, target);
            target.Id.Should().Be(7);
            target.Name.Should().Be("dyn");
        }

        [Fact]
        public void PocoToExpando() {
            var source = new Person { Id = 3, Name = "P" };
            dynamic target = new ExpandoObject();
            source.CloneXTo((ExpandoObject)target);
            Assert.Equal(3, target.Id);
            Assert.Equal("P", target.Name);
        }
    }
}
