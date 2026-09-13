using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class CreateInstanceCoverageTests {

        public abstract class AbstractType {
            public int Id { get; set; }
        }

        public interface IMarker {
            int Id { get; set; }
        }

        public class Concrete : AbstractType, IMarker {
        }

        [Fact]
        public void AbstractTypeReturnsNull() {
            CloneXExtensions.CreateInstance<AbstractType>().Should().BeNull();
        }

        [Fact]
        public void InterfaceReturnsNull() {
            CloneXExtensions.CreateInstance<IMarker>().Should().BeNull();
        }

        [Fact]
        public void MultiDimArrayWithoutTemplate() {
            var created = CloneXExtensions.CreateInstance<int[,]>();
            created.Should().NotBeNull();
            created.GetLength(0).Should().Be(0);
            created.GetLength(1).Should().Be(0);
        }

        [Fact]
        public void ArrayFromTemplateKeepsLength() {
            var template = new[] { 1, 2, 3 };
            var created = CloneXExtensions.CreateInstance(template);
            created.Should().NotBeSameAs(template);
            created.Should().HaveCount(3);
        }

        [Fact]
        public void MultiDimArrayFromTemplateKeepsBounds() {
            var template = new int[2, 3];
            var created = CloneXExtensions.CreateInstance(template);
            created.GetLength(0).Should().Be(2);
            created.GetLength(1).Should().Be(3);
        }

        [Fact]
        public void GenericListFromTemplateIsEmpty() {
            var template = new List<string> { "a" };
            var created = CloneXExtensions.CreateInstance(template);
            created.Should().NotBeSameAs(template);
            created.Should().BeEmpty();
        }
    }
}
