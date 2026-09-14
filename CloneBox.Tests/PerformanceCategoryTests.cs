using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Xunit;

namespace CloneBox.Tests {
    public class PerformanceCategoryTests {

        public class SimplePoco {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime Created { get; set; }
        }

        public class NestedPoco {
            public int Id { get; set; }
            public SimplePoco Child { get; set; }
            public NestedPoco Parent { get; set; }
            public List<SimplePoco> Items { get; set; }
        }

        private static double MeasureAfterWarmup<T>(T sample, Func<T, T> clone, int iterations) {
            clone(sample);
            var copies = new T[iterations];
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                copies[i] = clone(sample);
            sw.Stop();
            GC.KeepAlive(copies);
            return sw.Elapsed.TotalMilliseconds / iterations;
        }

        [Fact]
        public void SimplePoco_AfterWarmup() {
            var sample = new SimplePoco { Id = 1, Name = "A", Created = DateTime.UtcNow };
            MeasureAfterWarmup(sample, x => x.CloneX(), 2000).Should().BeLessThan(0.2);
        }

        [Fact]
        public void NestedPoco_AfterWarmup() {
            var sample = CreateNested();
            MeasureAfterWarmup(sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void ExpandoObject_AfterWarmup() {
            dynamic sample = new ExpandoObject();
            sample.Id = 1;
            sample.Name = "dyn";
            sample.Values = new List<int> { 1, 2, 3 };
            MeasureAfterWarmup((ExpandoObject)sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void PrimitiveArray_AfterWarmup() {
            var sample = new byte[1024];
            new Random(1).NextBytes(sample);
            MeasureAfterWarmup(sample, x => x.CloneX(), 2000).Should().BeLessThan(0.2);
        }

        [Fact]
        public void ClassArray_AfterWarmup() {
            var sample = new SimplePoco[50];
            for (int i = 0; i < sample.Length; i++)
                sample[i] = new SimplePoco { Id = i, Name = "n" + i, Created = DateTime.UtcNow };
            MeasureAfterWarmup(sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void List_AfterWarmup() {
            var sample = new List<SimplePoco>();
            for (int i = 0; i < 50; i++)
                sample.Add(new SimplePoco { Id = i, Name = "n" + i, Created = DateTime.UtcNow });
            MeasureAfterWarmup(sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void Dictionary_AfterWarmup() {
            var sample = new Dictionary<string, SimplePoco>();
            for (int i = 0; i < 50; i++)
                sample.Add(i.ToString(), new SimplePoco { Id = i, Name = "n" + i, Created = DateTime.UtcNow });
            MeasureAfterWarmup(sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void MultiDimArray_AfterWarmup() {
            var sample = new int[20, 20];
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                    sample[i, j] = i * 20 + j;
            MeasureAfterWarmup(sample, x => x.CloneX(), 500).Should().BeLessThan(1);
        }

        [Fact]
        public void CloneXTo_AfterWarmup() {
            var sample = new SimplePoco { Id = 1, Name = "A", Created = DateTime.UtcNow };
            sample.CloneXTo(new SimplePoco());
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
                sample.CloneXTo(new SimplePoco());
            sw.Stop();
            (sw.Elapsed.TotalMilliseconds / 2000).Should().BeLessThan(0.2);
        }

        [Fact]
        public void FirstCloneIsAllowedToBeSlowerThanCachedClone() {
            var first = new UniqueForCompile { Value = 1 };
            var firstMs = TimeOne(() => first.CloneX());
            var cachedMs = MeasureAfterWarmup(first, x => x.CloneX(), 200);
            cachedMs.Should().BeLessThan(firstMs * 50 + 1);
        }

        public class UniqueForCompile {
            public int Value { get; set; }
        }

        private static NestedPoco CreateNested() {
            var root = new NestedPoco { Id = 1, Items = new List<SimplePoco>() };
            root.Child = new SimplePoco { Id = 2, Name = "child", Created = DateTime.UtcNow };
            root.Parent = root;
            for (int i = 0; i < 10; i++)
                root.Items.Add(new SimplePoco { Id = i, Name = "i" + i, Created = DateTime.UtcNow });
            return root;
        }

        private static double TimeOne(Action action) {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }
    }
}
