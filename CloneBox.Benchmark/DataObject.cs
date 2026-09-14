namespace CloneBox.Benchmark {

    public class DataObject {
        public int Id { get; set; }
        public byte[]? Data { get; set; }
        public string? Name { get; set; }
        public ComplexGraph? Parent { get; set; }

        public static DataObject Create(ComplexGraph parent, int id) => new() {
            Id = id,
            Data = ComplexGraph.RandomBytes(1000),
            Name = ComplexGraph.RandomString(1000),
            Parent = parent
        };
    }
}
