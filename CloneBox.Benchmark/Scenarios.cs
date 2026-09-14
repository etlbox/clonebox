using System.Dynamic;

namespace CloneBox.Benchmark {

    internal sealed class Scenario {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public required string Blurb { get; init; }
        public required int Clones { get; init; }
        public required int Pool { get; init; }
        public required Func<int, object> Create { get; init; }
        public required Func<object?, object, string[]> Inspect { get; init; }
    }

    internal static class Scenarios {

        public static Scenario[] All() => [Simple(), Complex(), Dynamic(), Into()];

        static Scenario Simple() => new() {
            Id = "simple",
            Title = "1  Simple POCO",
            Blurb = "typed POCO: primitives, Tags list, nested SimpleChild  \u00b7  1,000,000 clones",
            Clones = 1_000_000,
            Pool = 2_000,
            Create = SimplePoco.Create,
            Inspect = InspectSimple
        };

        static Scenario Complex() => new() {
            Id = "complex",
            Title = "2  Cyclic graph",
            Blurb = $"1 root + {ComplexGraph.Children} children in List/Array/Dictionary (int keys), byte[], SelfReference cycle  \u00b7  400 clones",
            Clones = 400,
            Pool = 400,
            Create = ComplexGraph.Create,
            Inspect = InspectComplex
        };

        static Scenario Dynamic() => new() {
            Id = "dynamic",
            Title = "3  ExpandoObject",
            Blurb = "Expando as root: primitives, typed SimpleChild, nested Expando, Parent/Self cycles  \u00b7  300,000 clones",
            Clones = 300_000,
            Pool = 1_000,
            Create = DynamicGraph.Create,
            Inspect = InspectDynamic
        };

        static Scenario Into() => new() {
            Id = "into",
            Title = "4  CloneXTo  (Expando \u2192 existing DTO)",
            Blurb = "Expando \u2192 existing CustomerDto: matching members, keep ExtraOnTarget, skip [DoNotClone] Password  \u00b7  200,000 runs",
            Clones = 200_000,
            Pool = 1_000,
            Create = IntoDtoCase.Create,
            Inspect = InspectInto
        };

        static string[] InspectSimple(object? cloneObj, object srcObj) {
            if (cloneObj is not SimplePoco clone) return ["not a SimplePoco"];
            var src = (SimplePoco)srcObj;
            var issues = new List<string>();
            if (ReferenceEquals(clone, src)) issues.Add("same instance");
            if (clone.Id != src.Id || clone.Name != src.Name || clone.Amount != src.Amount || clone.Created != src.Created)
                issues.Add("values differ");
            if (clone.Tags.Count != src.Tags.Count || clone.Tags[0] != src.Tags[0])
                issues.Add("tags not copied");
            if (clone.Child == null) issues.Add("child missing");
            else if (ReferenceEquals(clone.Child, src.Child)) issues.Add("child shared");
            else if (clone.Child.Id != src.Child!.Id || clone.Child.Label != src.Child.Label)
                issues.Add("child values differ");
            return [.. issues];
        }

        static string[] InspectComplex(object? cloneObj, object srcObj) {
            if (cloneObj is not ComplexGraph clone) return ["not a ComplexGraph"];
            var src = (ComplexGraph)srcObj;
            var issues = new List<string>();
            if (ReferenceEquals(clone, src)) issues.Add("same instance");
            if (clone.Id != src.Id || clone.PI != src.PI || clone.CreationTime != src.CreationTime)
                issues.Add("property values differ");
            if (clone.longString != src.longString) issues.Add("public field skipped");
            if (clone.List.Count != src.List.Count || clone.Array.Length != src.Array.Length)
                issues.Add("collection size mismatch");
            else if (clone.List.Count > 0) {
                var a = src.List[0];
                var b = clone.List[0];
                if (ReferenceEquals(b, a)) issues.Add("children shared");
                else if (b.Name != a.Name) issues.Add("child values differ");
                if (b.Data == null || a.Data == null || !b.Data.AsSpan().SequenceEqual(a.Data))
                    issues.Add("byte[] not copied");
                else if (ReferenceEquals(b.Data, a.Data)) issues.Add("byte[] shared");
                if (ReferenceEquals(b.Parent, src)) issues.Add("Parent still original");
                else if (!ReferenceEquals(b.Parent, clone)) issues.Add("Parent cycle lost");
            }
            if (ReferenceEquals(clone.SelfReference, src)) issues.Add("SelfReference still original");
            else if (!ReferenceEquals(clone.SelfReference, clone)) issues.Add("SelfReference cycle lost");
            if (clone.Dictionary.Count != src.Dictionary.Count)
                issues.Add($"dictionary {clone.Dictionary.Count}/{src.Dictionary.Count}");
            else if (clone.Dictionary.Count > 0) {
                var sk = src.Dictionary.Keys.First();
                var ck = clone.Dictionary.Keys.First();
                if (sk.GetType() != ck.GetType())
                    issues.Add($"dict key {sk.GetType().Name}\u2192{ck.GetType().Name}");
            }
            return [.. issues];
        }

        static string[] InspectDynamic(object? cloneObj, object srcObj) {
            if (cloneObj is not ExpandoObject) return ["not an ExpandoObject (got " + (cloneObj?.GetType().Name ?? "null") + ")"];
            dynamic clone = cloneObj;
            dynamic src = srcObj;
            var issues = new List<string>();
            if (ReferenceEquals(cloneObj, srcObj)) issues.Add("same instance");
            if ((int)clone.Id != (int)src.Id || (string)clone.Name != (string)src.Name)
                issues.Add("values differ");

            if (clone.Child is not SimpleChild child) issues.Add("nested class missing");
            else if (ReferenceEquals(child, (object)src.Child)) issues.Add("nested class shared");
            else if (child.Id != (int)src.Child.Id || child.Label != (string)src.Child.Label)
                issues.Add("nested class values differ");

            if (clone.Items is not List<SimpleChild> items) issues.Add("Items list missing");
            else if (ReferenceEquals(items, (object)src.Items)) issues.Add("Items list shared");
            else if (items.Count != 2 || ReferenceEquals(items[0], (object)src.Items[0]))
                issues.Add("Items entries shared or lost");

            if (clone.Nested is not ExpandoObject) issues.Add("nested Expando missing");
            else if (ReferenceEquals((object)clone.Nested, (object)src.Nested)) issues.Add("nested Expando shared");
            else {
                if ((string)clone.Nested.Value != (string)src.Nested.Value)
                    issues.Add("nested Expando values differ");
                if (clone.Nested.Child is not SimpleChild nestedChild) issues.Add("class inside nested Expando missing");
                else if (ReferenceEquals(nestedChild, (object)src.Nested.Child))
                    issues.Add("class inside nested Expando shared");
                if (ReferenceEquals((object)clone.Nested.Parent, srcObj))
                    issues.Add("Nested.Parent still original");
                else if (!ReferenceEquals((object)clone.Nested.Parent, cloneObj))
                    issues.Add("Nested.Parent cycle lost");
            }

            if (ReferenceEquals((object)clone.Self, srcObj)) issues.Add("Self still original");
            else if (!ReferenceEquals((object)clone.Self, cloneObj)) issues.Add("Self cycle lost");

            string original = src.Name;
            clone.Name = "MUTATED";
            if ((string)src.Name == "MUTATED") issues.Add("Expando storage shared with original");
            clone.Name = original;
            return [.. issues];
        }

        static string[] InspectInto(object? cloneObj, object srcObj) {
            if (cloneObj is not CustomerDto clone)
                return ["not a CustomerDto (no clone-into)"];
            dynamic src = ((IntoDtoCase)srcObj).Source;
            var issues = new List<string>();
            if (clone.Id != (int)src.Id || clone.Name != (string)src.Name)
                issues.Add("DTO values differ");
            if (clone.Child is not SimpleChild child) issues.Add("nested class missing");
            else if (ReferenceEquals(child, (object)src.Child)) issues.Add("nested class shared");
            else if (child.Id != (int)src.Child.Id || child.Label != (string)src.Child.Label)
                issues.Add("nested class values differ");
            if (clone.Password != "already-set")
                issues.Add("[DoNotClone] Password was overwritten");
            if (clone.ExtraOnTarget != "keep-me")
                issues.Add("target-only ExtraOnTarget was lost");
            return [.. issues];
        }
    }
}
