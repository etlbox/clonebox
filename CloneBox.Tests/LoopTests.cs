using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class LoopTests {

        public class LOOP {
            public int Id { get; set; }

            public LOOP SelfRef { get; set; }
        }

        [Fact]
        public void ReferenceLoop() {
            var loop1 = new LOOP() { Id = 1 };            
            var loop2 = new LOOP() { Id = 2 };
            loop1.SelfRef = loop2;
            loop2.SelfRef = loop1;
            var clone = loop1.CloneX();

            clone.Should().NotBeSameAs(loop1);
            clone.SelfRef.Should().NotBeSameAs(loop2);

            clone.Should().NotBeNull();
            clone.SelfRef.Should().NotBeNull();
            clone.SelfRef.SelfRef.Should().BeSameAs(clone);
        }

        [Fact]
        public void NestedLoop() {
            var loop = new LOOP() { Id = 1 };
            loop.SelfRef = loop;
            var clone = loop.CloneX();

            clone.Should().NotBeSameAs(loop);
            clone.SelfRef.Should().NotBeSameAs(loop);
            clone.SelfRef.Should().NotBeNull();
            clone.SelfRef.Should().BeSameAs(clone);
            clone.SelfRef.SelfRef.Should().BeSameAs(clone);
        }
    }
}
