using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace CloneBox.Tests {
    public class CycleCollectionTests {

        public class Node {
            public int Id { get; set; }
            public List<Node> Children { get; set; }
            public Node[] Peers { get; set; }
            public Dictionary<string, Node> Map { get; set; }
            public Node Next { get; set; }
        }

        [Fact]
        public void CycleThroughList() {
            var root = new Node { Id = 1, Children = new List<Node>() };
            root.Children.Add(root);
            var clone = root.CloneX();
            clone.Should().NotBeSameAs(root);
            clone.Children.Should().NotBeSameAs(root.Children);
            clone.Children[0].Should().BeSameAs(clone);
        }

        [Fact]
        public void CycleThroughArray() {
            var root = new Node { Id = 1 };
            root.Peers = new[] { root };
            var clone = root.CloneX();
            clone.Peers[0].Should().BeSameAs(clone);
            clone.Peers.Should().NotBeSameAs(root.Peers);
        }

        [Fact]
        public void CycleThroughDictionary() {
            var root = new Node { Id = 1, Map = new Dictionary<string, Node>() };
            root.Map["self"] = root;
            var clone = root.CloneX();
            clone.Map["self"].Should().BeSameAs(clone);
            clone.Map.Should().NotBeSameAs(root.Map);
        }

        [Fact]
        public void ThreeNodeCycle() {
            var a = new Node { Id = 1 };
            var b = new Node { Id = 2 };
            var c = new Node { Id = 3 };
            a.Next = b;
            b.Next = c;
            c.Next = a;
            var clone = a.CloneX();
            clone.Id.Should().Be(1);
            clone.Next.Id.Should().Be(2);
            clone.Next.Next.Id.Should().Be(3);
            clone.Next.Next.Next.Should().BeSameAs(clone);
            clone.Should().NotBeSameAs(a);
        }

        [Fact]
        public void SharedNodeInListAndDictionary() {
            var shared = new Node { Id = 7 };
            var root = new Node {
                Id = 1,
                Children = new List<Node> { shared },
                Map = new Dictionary<string, Node> { { "x", shared } }
            };
            var clone = root.CloneX();
            clone.Children[0].Should().BeSameAs(clone.Map["x"]);
            clone.Children[0].Should().NotBeSameAs(shared);
        }
    }
}
