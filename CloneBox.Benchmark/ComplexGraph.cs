namespace CloneBox.Benchmark {

    public class ComplexGraph {

        const int CollectionSize = 100;
        public const int Children = CollectionSize * 3 + 1;

        public int? Id { get; set; }
        public string? longString;
        public double? PI;
        public DateTime? CreationTime { get; set; }
        public List<DataObject> List { get; set; } = new();
        public DataObject[] Array { get; set; } = new DataObject[CollectionSize];
        public Dictionary<object, DataObject> Dictionary { get; set; } = new();
        public DataObject? DataObject { get; set; }
        public object? SelfReference { get; set; }

        public static ComplexGraph Create(int id) {
            var res = new ComplexGraph {
                Id = id,
                longString = RandomString(1000),
                PI = Math.PI,
                CreationTime = DateTime.Now
            };
            for (var i = 0; i < CollectionSize; i++) {
                res.List.Add(DataObject.Create(res, id));
                res.Array[i] = DataObject.Create(res, id);
                res.Dictionary.Add(i, DataObject.Create(res, id));
            }
            res.DataObject = DataObject.Create(res, id);
            res.SelfReference = res;
            return res;
        }

        static readonly Random Random = new(20260914);

        public static string RandomString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var buffer = new char[length];
            for (var i = 0; i < length; i++)
                buffer[i] = chars[Random.Next(chars.Length)];
            return new string(buffer);
        }

        public static byte[] RandomBytes(int length) {
            var buffer = new byte[length];
            Random.NextBytes(buffer);
            return buffer;
        }
    }
}
