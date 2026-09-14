namespace CloneBox.Benchmark {

    public class SimplePoco {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime Created { get; set; }
        public decimal Amount { get; set; }
        public List<string> Tags { get; set; } = new();
        public SimpleChild? Child { get; set; }

        public static SimplePoco Create(int id) => new() {
            Id = id,
            Name = "item-" + id,
            Created = new DateTime(2026, 1, 1).AddMinutes(id),
            Amount = 12.34m + id,
            Tags = ["red", "green", "blue"],
            Child = new SimpleChild { Id = id * 10, Label = "child-" + id }
        };
    }

    public class SimpleChild {
        public int Id { get; set; }
        public string? Label { get; set; }
    }
}
