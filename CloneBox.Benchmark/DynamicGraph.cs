using System.Dynamic;

namespace CloneBox.Benchmark {

    /// <summary>
    /// The root <em>is</em> an ExpandoObject (not a typed wrapper). Members are mixed:
    /// primitives, a real class, a nested ExpandoObject, and a cycle back to the root.
    /// </summary>
    internal static class DynamicGraph {

        public static ExpandoObject Create(int id) {
            dynamic root = new ExpandoObject();
            root.Id = id;
            root.Name = "expando-" + id;
            root.Child = new SimpleChild { Id = id, Label = "typed-child" };
            root.Items = new List<SimpleChild> {
                new() { Id = id, Label = "item-a" },
                new() { Id = id + 1, Label = "item-b" }
            };

            dynamic nested = new ExpandoObject();
            nested.Value = "inner-" + id;
            nested.Child = new SimpleChild { Id = id + 10, Label = "nested-typed" };
            nested.Parent = root;
            root.Nested = nested;
            root.Self = root;
            return root;
        }
    }
}
