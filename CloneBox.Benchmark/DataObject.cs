using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneBox.Benchmark {
    public class DataObject {
        public int Id { get; set; }
        public byte[]? Data { get; set; }
        public string? Name { get; set; }

        public BenchmarkObject? Parent { get; set; } 


        public static DataObject CreateDataObject(BenchmarkObject parent, int id) {
            return new DataObject() {
                Id = id,
                Data = Encoding.GetEncoding("ISO-8859-1").GetBytes(StringHelper.RandomString(1000)),
                Name = StringHelper.RandomString(1000),
                Parent = parent
            };
        }
    }
}
