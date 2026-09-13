using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class CloneSettingsCoverageTests {

        public class Cloneable : ICloneable {
            public bool CloneCalled;
            public int Id { get; set; }

            public object Clone() {
                CloneCalled = true;
                return new Cloneable { Id = 99 };
            }
        }

        public class MixedAccess {
            public int PublicProp { get; set; }
            internal int InternalProp { get; set; }
            public int PublicField;
            internal int InternalField;
            public MixedAccess() { }
            private MixedAccess(int unused) { PublicProp = unused; }
        }

        [Fact]
        public void ICloneableIsIgnoredByDefault() {
            var orig = new Cloneable { Id = 1 };
            var clone = orig.CloneX();
            orig.CloneCalled.Should().BeFalse();
            clone.Should().NotBeSameAs(orig);
            clone.Id.Should().Be(1);
            clone.CloneCalled.Should().BeFalse();
        }

        [Fact]
        public void IncludePublicPropertiesFalseCopiesOnlyFields() {
            var orig = new MixedAccess { PublicProp = 1, InternalProp = 2, PublicField = 3, InternalField = 4 };
            var clone = orig.CloneX(new CloneSettings {
                IncludePublicProperties = false,
                IncludeNonPublicProperties = false,
                IncludeNonPublicFields = false
            });
            clone.PublicProp.Should().Be(0);
            clone.PublicField.Should().Be(3);
            clone.InternalField.Should().Be(0);
        }

        [Fact]
        public void IncludePublicConstructorsFalseStillUsesNonPublicConstructor() {
            var orig = MixedAccessFactory();
            orig.PublicProp = 42;
            var clone = orig.CloneX(new CloneSettings {
                IncludePublicConstructors = false,
                IncludeNonPublicConstructors = true
            });
            clone.Should().NotBeNull();
            clone.PublicProp.Should().Be(42);
        }

        [Fact]
        public void IncludeAllConstructorsFalseReturnsNullWhenNoUsableConstructor() {
            var orig = OnlyPrivateCtor.Create();
            orig.Value = 7;
            var clone = orig.CloneX(new CloneSettings {
                IncludePublicConstructors = false,
                IncludeNonPublicConstructors = false
            });
            clone.Should().BeNull();
        }

        [Fact]
        public void NullSettingsAreTreatedAsDefaults() {
            var orig = new MixedAccess { PublicProp = 5, PublicField = 6 };
            var clone = orig.CloneX(null);
            clone.PublicProp.Should().Be(5);
            clone.PublicField.Should().Be(6);
        }

        [Fact]
        public void DifferentSettingsProduceDifferentMemberSetsForSameType() {
            var orig = new MixedAccess { PublicProp = 1, InternalProp = 2, PublicField = 3, InternalField = 4 };
            var publicOnly = orig.CloneX(new CloneSettings {
                IncludeNonPublicProperties = false,
                IncludeNonPublicFields = false
            });
            var all = orig.CloneX();
            publicOnly.InternalProp.Should().Be(0);
            all.InternalProp.Should().Be(2);
        }

        [Fact]
        public void LoggerIsInvokedWhenEnumerableCannotBeFilled() {
            var logger = new CapturingLogger();
            var orig = new SortedList<int, string> { { 1, "a" }, { 2, "b" } };
            var clone = orig.Values.CloneX(new CloneSettings { Logger = logger });
            clone.Should().BeSameAs(orig.Values);
            logger.Count.Should().BeGreaterThan(0);
        }

        public class OnlyPrivateCtor {
            private OnlyPrivateCtor() { }
            public static OnlyPrivateCtor Create() => new OnlyPrivateCtor();
            public int Value { get; set; }
        }

        private static MixedAccess MixedAccessFactory() => new MixedAccess();

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
