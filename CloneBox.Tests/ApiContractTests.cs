using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class ApiContractTests {

        public class Person {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Extra;
        }

        public class OnlyPrivateCtor {
            private OnlyPrivateCtor() { }
            public static OnlyPrivateCtor Create() => new OnlyPrivateCtor();
            public int Value { get; set; }
        }

        [Fact]
        public void TwoIndependentClonesDoNotShareIdentity() {
            var orig = new Person { Id = 1, Name = "A" };
            var first = orig.CloneX();
            var second = orig.CloneX();
            first.Should().NotBeSameAs(second);
            first.Should().NotBeSameAs(orig);
            first.Name.Should().Be("A");
            second.Name.Should().Be("A");
        }

        [Fact]
        public void CloneXToWithoutDestinationOnNullSourceReturnsNull() {
            Person source = null;
            source.CloneXTo<Person, Person>(new CloneSettings()).Should().BeNull();
        }

        [Fact]
        public void CloneXToThrowsOnNullSourceWhenDestinationIsGiven() {
            Person source = null;
            var target = new Person();
            Action act = () => source.CloneXTo(target);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AllMemberFlagsFalseLeavesValuesAtDefaults() {
            var orig = new Person { Id = 4, Name = "A", Extra = 9 };
            var clone = orig.CloneX(new CloneSettings {
                IncludePublicProperties = false,
                IncludePublicFields = false,
                IncludeNonPublicProperties = false,
                IncludeNonPublicFields = false
            });
            clone.Should().NotBeNull();
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(0);
            clone.Name.Should().BeNull();
            clone.Extra.Should().Be(0);
        }

        [Fact]
        public void IncludePublicFieldsFalseSkipsFields() {
            var orig = new Person { Id = 4, Name = "A", Extra = 9 };
            var clone = orig.CloneX(new CloneSettings { IncludePublicFields = false });
            clone.Id.Should().Be(4);
            clone.Name.Should().Be("A");
            clone.Extra.Should().Be(0);
        }

        [Fact]
        public void DoNotCloneClassOnTargetReturnsNull() {
            var source = new Person { Id = 1, Name = "A" };
            var target = new Person();
            var clone = source.CloneXTo(target, new CloneSettings {
                DoNotCloneClass = t => t == typeof(Person)
            });
            clone.Should().BeNull();
            target.Id.Should().Be(0);
        }

        [Fact]
        public void DoNotCloneFieldPredicate() {
            var orig = new Person { Id = 1, Name = "A", Extra = 9 };
            var clone = orig.CloneX(new CloneSettings {
                DoNotCloneField = f => f.Name == "Extra"
            });
            clone.Id.Should().Be(1);
            clone.Name.Should().Be("A");
            clone.Extra.Should().Be(0);
        }

        [Fact]
        public void DoNotCloneAttributeIsHonoredOnCloneXTo() {
            var source = new Marked { Keep = "A", Skip = "B" };
            var target = new Marked { Keep = "x", Skip = "y" };
            source.CloneXTo(target);
            target.Keep.Should().Be("A");
            target.Skip.Should().Be("y");
        }

        public class Marked {
            public string Keep { get; set; }
            [DoNotClone]
            public string Skip { get; set; }
        }

        [Fact]
        public void LoggerIsNotInvokedOnSuccessfulPocoClone() {
            var logger = new CapturingLogger();
            var orig = new Person { Id = 1, Name = "A" };
            orig.CloneX(new CloneSettings { Logger = logger });
            logger.Count.Should().Be(0);
        }

        [Fact]
        public void CreateInstanceForEnumAndPrivateCtor() {
            CloneXExtensions.CreateInstance<DayOfWeek>().Should().Be(DayOfWeek.Sunday);
            var created = CloneXExtensions.CreateInstance<OnlyPrivateCtor>();
            created.Should().NotBeNull();
            created.Value.Should().Be(0);
        }

        [Fact]
        public void CreateInstanceHashSetIsEmpty() {
            CloneXExtensions.CreateInstance<HashSet<int>>().Should().BeEmpty();
        }

        [Fact]
        public void CreateInstanceWithNullTemplateStillCreates() {
            Person template = null;
            var created = CloneXExtensions.CreateInstance(template);
            created.Should().NotBeNull();
            created.Id.Should().Be(0);
        }

        private sealed class CapturingLogger : ILogger {
            public int Count { get; private set; }
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
                Count++;
            }
        }
    }
}
