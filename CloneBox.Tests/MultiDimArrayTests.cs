using FluentAssertions;
using FluentAssertions.Equivalency.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CloneBox.Tests {
    public class MultiDimArrayTests {

        [Fact]
        public void TwoDimArray() {
            var orig = new int[2, 2];
            orig[0, 0] = 1;
            orig[0, 1] = 2;
            orig[1, 0] = 3;
            orig[1, 1] = 4;
            var clone = orig.CloneX();

            clone.Should().NotBeSameAs(orig);
            clone[0, 0].Should().Be(1);
            clone[0, 1].Should().Be(2);
            clone[1, 0].Should().Be(3);
            clone[1, 1].Should().Be(4);
        }

        [Fact]
        public void ThreeDimArrayWithNullable() {
            var orig = new int?[2, 2, 1];
            orig[0, 0, 0] = 1;
            orig[0, 1, 0] = 2;
            orig[1, 0, 0] = null;
            orig[1, 1, 0] = 4;
            var clone = orig.CloneX();

            clone.Should().NotBeSameAs(orig);
            clone[0, 0,0].Should().Be(1);
            clone[0, 1,0].Should().Be(2);
            clone[1, 0,0].Should().BeNull();
            clone[1, 1,0].Should().Be(4);
        }

        [Fact]
        public void ThreeDimArray() {
            const int cnt1 = 4;
            const int cnt2 = 5;
            const int cnt3 = 6;
            var orig = new int[cnt1, cnt2, cnt3];
            for (var i1 = 0; i1 < cnt1; i1++)
                for (var i2 = 0; i2 < cnt2; i2++)
                    for (var i3 = 0; i3 < cnt3; i3++)
                        orig[i1, i2, i3] = i1 * 100 + i2 * 10 + i3;
            var clone = orig.CloneX();
            clone.Should().NotBeSameAs(orig);
            for (var i1 = 0; i1 < cnt1; i1++)
                for (var i2 = 0; i2 < cnt2; i2++)
                    for (var i3 = 0; i3 < cnt3; i3++)
                        orig[i1, i2, i3].Should().Be(i1 * 100 + i2 * 10 + i3);
        }


        public class ARROBJ {
            public int[] A { get; set; }

            public int[] B { get; set; }
        }


        [Fact]
        public void TwoDimWithClass() {
            var orig = new ARROBJ[2, 2];
            var rec1 = new ARROBJ();
            var rec2 = new ARROBJ() { A = new[] { 1, 2 }, B = new[] { 2 } };
            orig[0, 0] = rec1;
            orig[1, 1] = rec2;
            orig[0, 1] = rec2;

            var clone = orig.CloneX();

            clone.Should().NotBeSameAs(orig);
            clone[0, 0].Should().NotBeNull();
            clone[1, 0].Should().BeNull();
            clone[1, 1].Should().NotBeNull();
            clone[0, 1].Should().NotBeNull();

            clone[0, 0].Should().NotBeSameAs(clone[1,1]);
            clone[1, 1].Should().BeSameAs(clone[0, 1]);

            clone[1, 1].A.Should().BeEquivalentTo(new[] { 1, 2 });
        }


        [Fact]
        public void NonZeroBased() {
            var orig = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 });            
            orig.SetValue(1, 1);
            orig.SetValue(2, 2);
            var clone = orig.CloneX();
            clone.GetValue(1).Should().Be(1);
            clone.GetValue(2).Should().Be(2);
        }

        [Fact]
        public void NonZeroBased2Dimensions() {
            var orig = Array.CreateInstance(typeof(string), new[] { 2,3 }, new[] { 1,2 });
            orig.SetValue("A", 1, 2);
            orig.SetValue("B", 1, 3);
            orig.SetValue("C", 2, 2);
            orig.SetValue("D", 2, 3);
            var clone = orig.CloneX();
            clone.GetValue(1,2).Should().Be("A");
            clone.GetValue(1,3).Should().Be("B");
            clone.GetValue(1,4).Should().BeNull();
            clone.GetValue(2, 2).Should().Be("C");
            clone.GetValue(2, 3).Should().Be("D");
            clone.GetValue(2, 4).Should().BeNull();
        }

        [Fact]
        public void NonZeroBased3Dimensions() {
            var orig = Array.CreateInstance(typeof(string), new[] { 2, 3,2 }, new[] { 1, 2, 1 });
            orig.SetValue("A", new [] {1,2,1});
            orig.SetValue("B", new[] { 1, 3, 1 });
            var clone = orig.CloneX();
            orig.GetValue(new[] { 1, 2, 1 }).Should().Be("A");
            orig.GetValue(new[] { 1, 3, 1 }).Should().Be("B");
            orig.GetValue(new[] { 1, 4, 1 }).Should().BeNull();
        }


    }
}
