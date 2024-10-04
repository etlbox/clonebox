using BenchmarkDotNet.Attributes;
using Force.DeepCloner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneBox.Benchmark {

    public class SimpleDataObject() {
        public int Field1 { get; set; }
        public string? Field2 { get; set; }
    }

    public class SimpleDataClone {

        [Params(100, 1000)]
        public int N;

        public List<SimpleDataObject> SourceData = new List<SimpleDataObject>();

        [GlobalSetup]
        public void Setup() {
            for (int i = 0; i < N; i++) {
                SourceData.Add(new SimpleDataObject() {
                    Field1 = i,
                    Field2 = StringHelper.RandomString(1000)
                });
            }
        }

        [Benchmark]
        public void SimpleWithCloneX() {
            var targetData = new List<SimpleDataObject>(SourceData.Count);
            foreach (var record in SourceData)
                targetData.Add(record.CloneX());
        }

        [Benchmark]
        public void SimpleWithDeepClone() {
            var targetData = new List<SimpleDataObject>(SourceData.Count);
            foreach (var record in SourceData)
                targetData.Add(record.DeepClone());
        }

    }
}
