using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneBox.Benchmark {
    public class BenchmarkObject {

        public int? Id { get; set; }
        public string? longString;
        public double? PI;
        public DateTime? CreationTime { get; set; }
        public List<DataObject> List { get; set; } = new List<DataObject>();
        public DataObject[] Array { get; set; } = new DataObject[10];
        public Dictionary<object, DataObject> Dictionary { get; set; } = new Dictionary<object, DataObject>();
        public DataObject? DataObject { get; set; } 
               
        public object? SelfReference { get; set; }

        public static BenchmarkObject CreateTestObject(int id) {
            var res = new BenchmarkObject() {
                Id = id,
                longString = StringHelper.RandomString(1000),
                PI = Math.PI,
                CreationTime = DateTime.Now
            };
            for (int i=0;i<res.Array.Length;i++) {
                //res.List.Add(DataObject.CreateDataObject(res, id));
                res.Array[i] = DataObject.CreateDataObject(res, id);
                //res.Dictionary.Add(i, DataObject.CreateDataObject(res, id));
            }
            res.DataObject = DataObject.CreateDataObject(res, id);
            res.SelfReference = res;
            return res;
        }
        
        
    }
}
