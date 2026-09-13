using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace CloneBox.Tests {
    public class PerformanceTests {

        public class DataNode {
            public int Id { get; set; }
            public byte[] Data { get; set; }
            public string Name { get; set; }
            public GraphNode Parent { get; set; }
        }

        public class GraphNode {
            public int? Id { get; set; }
            public string LongString;
            public double? Pi;
            public DateTime? CreationTime { get; set; }
            public List<DataNode> List { get; set; } = new List<DataNode>();
            public DataNode[] Array { get; set; } = new DataNode[20];
            public Dictionary<object, DataNode> Dictionary { get; set; } = new Dictionary<object, DataNode>();
            public DataNode DataObject { get; set; }
            public object SelfReference { get; set; }

            public static GraphNode Create(int id) {
                var node = new GraphNode {
                    Id = id,
                    LongString = new string('A', 64),
                    Pi = Math.PI,
                    CreationTime = DateTime.UtcNow
                };
                for (int i = 0; i < node.Array.Length; i++) {
                    var child = new DataNode { Id = id, Data = new byte[64], Name = "n" + i, Parent = node };
                    node.List.Add(child);
                    node.Array[i] = child;
                    node.Dictionary.Add(i, new DataNode { Id = id, Data = new byte[64], Name = "d" + i, Parent = node });
                }
                node.DataObject = new DataNode { Id = id, Data = new byte[64], Name = "root", Parent = node };
                node.SelfReference = node;
                return node;
            }
        }

        [Fact]
        public void CompiledClonerStaysFastAfterWarmup() {
            var settings = new CloneSettings {
                IncludeNonPublicFields = false,
                IncludeNonPublicProperties = false,
                IncludeNonPublicConstructors = false
            };
            var warmup = GraphNode.Create(0);
            warmup.CloneX(settings);

            const int iterations = 200;
            var sources = new GraphNode[iterations];
            for (int i = 0; i < iterations; i++)
                sources[i] = GraphNode.Create(i);

            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                sources[i].CloneX(settings);
            timer.Stop();

            var millisecondsPerClone = timer.Elapsed.TotalMilliseconds / iterations;
            millisecondsPerClone.Should().BeLessThan(15);
        }
    }
}
